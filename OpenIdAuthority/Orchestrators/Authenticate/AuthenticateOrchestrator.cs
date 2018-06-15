// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Models;
using SimpleIAM.OpenIdAuthority.Services;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;

namespace SimpleIAM.OpenIdAuthority.Orchestrators
{
    public class AuthenticateOrchestrator : ActionResponder
    {
        private enum SignInMethod
        {
            Password,
            OneTimeCode,
            Link
        }
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly IUserStore _userStore;
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IPasswordService _passwordService;
        private readonly IdProviderConfig _config;
        private readonly IUrlHelper _urlHelper;
        private readonly HttpContext _httpContext;
        private readonly IAuthorizedDeviceStore _authorizedDeviceStore;


        public AuthenticateOrchestrator(
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            IUserStore userStore,
            IdProviderConfig config,
            IClientStore clientStore,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IPasswordService passwordService,
            IUrlHelper urlHelper,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizedDeviceStore authorizedDeviceStore)
        {
            _oneTimeCodeService = oneTimeCodeService;
            _userStore = userStore;
            _messageService = messageService;
            _clientStore = clientStore;
            _passwordService = passwordService;
            _interaction = interaction;
            _events = events;
            _config = config;
            _urlHelper = urlHelper;
            _httpContext = httpContextAccessor.HttpContext;
            _authorizedDeviceStore = authorizedDeviceStore;
        }

        public async Task<ActionResponse> RegisterAsync(RegisterInputModel model)
        {
            if (!await ApplicationIdIsNullOrValidAsync(model.ApplicationId))
            {
                return BadRequest("Invalid application id");
            }

            TimeSpan linkValidity;
            var existingUser = await _userStore.GetUserByEmailAsync(model.Email);
            if (existingUser == null)
            {
                var newUser = new User()
                {
                    Email = model.Email,
                    Claims = model.Claims?.Select(x => new UserClaim() { Type = x.Key, Value = x.Value }) //todo: filter these to claim types that are allowed to be set by user
                };
                newUser = await _userStore.AddUserAsync(newUser);
                linkValidity = TimeSpan.FromHours(24);
            }
            else
            {
                linkValidity = TimeSpan.FromMinutes(5);
                //may want allow admins to configure a different email to send to existing users. However, it could be that the user
                // exists but just never got a welcome email?
            }

            var nextUrl = !string.IsNullOrEmpty(model.NextUrl) ? model.NextUrl : _urlHelper.Action("Apps", "Home");
            if (model.InviteToSetPasword)
            {
                nextUrl = SendToSetPasswordFirst(nextUrl);
            }

            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Email, linkValidity, nextUrl);
            switch (oneTimeCodeResponse.Result)
            {
                case GetOneTimeCodeResult.Success:
                    var result = await _messageService.SendWelcomeMessageAsync(model.ApplicationId, model.Email, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode, model.Claims);
                    return ReturnAppropriateResponse(result, oneTimeCodeResponse.ClientNonce, "Thanks for registering. Please check your email.");
                case GetOneTimeCodeResult.TooManyRequests:
                    return BadRequest("Please wait a few minutes and try again");
                case GetOneTimeCodeResult.ServiceFailure:
                default:
                    return ServerError("Hmm, something went wrong. Can you try again?");
            }
        }

        public async Task<ActionResponse> SendOneTimeCodeAsync(SendCodeInputModel model)
        {
            if (!await ApplicationIdIsNullOrValidAsync(model.ApplicationId))
            {
                return BadRequest("Invalid application id");
            }

            // todo: support usernames/phone numbers
            // Note: Need to keep messages generic as to not reveal whether an account exists or not. 
            // If the username provide is not an email address or phone number, tell the user "we sent you a code if you have an account"
            if (model.Username?.Contains("@") == true) // temporary rough email check
            {
                var user = await _userStore.GetUserByEmailAsync(model.Username);
                if (user != null)
                {
                    var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Username, TimeSpan.FromMinutes(5), model.NextUrl);
                    switch (oneTimeCodeResponse.Result)
                    {
                        case GetOneTimeCodeResult.Success:
                            var result = await _messageService.SendOneTimeCodeAndLinkMessageAsync(model.ApplicationId, model.Username, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode);
                            return ReturnAppropriateResponse(result, oneTimeCodeResponse.ClientNonce, "Message sent. Please check your email.");
                        case GetOneTimeCodeResult.TooManyRequests:
                            return BadRequest("Please wait a few minutes before requesting a new code");
                        case GetOneTimeCodeResult.ServiceFailure:
                        default:
                            return ServerError("Hmm, something went wrong. Can you try again?");
                    }
                }
                else
                {
                    // if valid email or phone number, send a message inviting them to register
                    var result = await _messageService.SendAccountNotFoundMessageAsync(model.ApplicationId, model.Username);
                    if (!result.MessageSent)
                    {
                        return ServerError(result.ErrorMessageForEndUser);
                    }
                    return Ok("Message sent. Please check your email.");
                }
            }
            else
            {
                return BadRequest("Please enter a valid email address");
            }
        }

        public async Task<ActionResponse> AuthenticateAsync(AuthenticatePasswordInputModel model)
        {
            var oneTimeCode = model.Password.Replace(" ", "");
            if (oneTimeCode.Length == 6 && oneTimeCode.All(Char.IsDigit))
            {
                var input = new AuthenticateInputModel()
                {
                    Username = model.Username,
                    OneTimeCode = oneTimeCode,
                    StaySignedIn = model.StaySignedIn
                };
                return await AuthenticateCodeAsync(input);
            }
            else
            {
                return await AuthenticatePasswordAsync(model);
            }
        }

        public async Task<ActionResponse> AuthenticateCodeAsync(AuthenticateInputModel model)
        {
            model.OneTimeCode = model.OneTimeCode.Replace(" ", "");
            var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(model.Username, model.OneTimeCode, _httpContext.Request.GetClientNonce());
            switch (response.Result)
            {
                case CheckOneTimeCodeResult.VerifiedWithNonce:
                case CheckOneTimeCodeResult.VerifiedWithoutNonce:
                    var nonceWasValid = response.Result == CheckOneTimeCodeResult.VerifiedWithNonce;
                    return await SignInAndRedirectAsync(SignInMethod.OneTimeCode, model.Username, model.StaySignedIn, response.RedirectUrl, nonceWasValid);
                case CheckOneTimeCodeResult.Expired:
                    return Unauthenticated("Your one time code has expired. Please request a new one.");
                case CheckOneTimeCodeResult.CodeIncorrect:
                case CheckOneTimeCodeResult.NotFound:
                    return Unauthenticated("Invalid one time code");
                case CheckOneTimeCodeResult.ShortCodeLocked:
                    return Unauthenticated("The one time code is locked. Please request a new one after a few minutes.");
                case CheckOneTimeCodeResult.ServiceFailure:
                default:
                    return ServerError("Something went wrong.");
            }
        }

        public async Task<ActionResponse> AuthenticatePasswordAsync(AuthenticatePasswordInputModel model)
        {
            // todo: if device is not authorized need to do a partial login and send a code by email
            var user = await _userStore.GetUserByEmailAsync(model.Username); //todo: handle non-email addresses
            if (user == null)
            {
                return Unauthenticated("The email address or password wasn't right");
            }
            else
            {
                var checkPasswordResult = await _passwordService.CheckPasswordAsync(user.SubjectId, model.Password);
                switch (checkPasswordResult)
                {
                    case CheckPasswordResult.NotFound:
                    case CheckPasswordResult.PasswordIncorrect:
                        return Unauthenticated("The email address or password wasn't right");
                    case CheckPasswordResult.TemporarilyLocked:
                        return Unauthenticated("Your password is temporarily locked. Use a one time code to sign in.");
                    case CheckPasswordResult.Success:
                        return await SignInAndRedirectAsync(SignInMethod.Password, model.Username, model.StaySignedIn, model.NextUrl, null);
                    case CheckPasswordResult.ServiceFailure:
                    default:
                        return ServerError("Hmm. Something went wrong. Please try again.");
                }
            }
        }

        public async Task<ActionResponse> AuthenticateLongCodeAsync(string longCode)
        {
            if (longCode != null && longCode.Length < 36)
            {
                var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(longCode, _httpContext.Request.GetClientNonce());
                switch (response.Result)
                {
                    case CheckOneTimeCodeResult.VerifiedWithoutNonce:
                    case CheckOneTimeCodeResult.VerifiedWithNonce:
                        var nonceWasValid = response.Result == CheckOneTimeCodeResult.VerifiedWithNonce;
                        return await SignInAndRedirectAsync(SignInMethod.Link, response.SentTo, null, response.RedirectUrl, nonceWasValid);
                    case CheckOneTimeCodeResult.Expired:
                        return Unauthenticated("The sign in link expired.");
                    case CheckOneTimeCodeResult.CodeIncorrect:
                    case CheckOneTimeCodeResult.NotFound:
                        return Unauthenticated("The sign in link is invalid.");
                    case CheckOneTimeCodeResult.ServiceFailure:
                    default:
                        return ServerError("Something went wrong.");
                }
            }
            return NotFound();
        }

        public async Task<ActionResponse> SendPasswordResetMessageAsync(SendPasswordResetMessageInputModel model)
        {
            if (!await ApplicationIdIsNullOrValidAsync(model.ApplicationId))
            {
                return BadRequest("Invalid application id");
            }

            var user = await _userStore.GetUserByEmailAsync(model.Username); //todo: support non-email addresses
            if (user == null)
            {
                // if valid email or phone number, send a message inviting them to register
                if (model.Username.Contains("@"))
                {
                    var result = await _messageService.SendAccountNotFoundMessageAsync(model.ApplicationId, model.Username);
                    if (!result.MessageSent)
                    {
                        return ServerError(result.ErrorMessageForEndUser);
                    }
                }
                return Ok("Check your email for password reset instructions.");
            }
            var nextUrl = SendToSetPasswordFirst(!string.IsNullOrEmpty(model.NextUrl) ? model.NextUrl : _urlHelper.Action("Apps", "Home"));
            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Username, TimeSpan.FromMinutes(5), nextUrl);
            if (oneTimeCodeResponse.Result == GetOneTimeCodeResult.Success)
            {
                var result = await _messageService.SendPasswordResetMessageAsync(model.ApplicationId, model.Username, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode);
                return ReturnAppropriateResponse(result, oneTimeCodeResponse.ClientNonce, "Check your email for password reset instructions.");
            }
            return ServerError("Hmm. Something went wrong. Please try again.");
        }

        private ActionResponse ReturnAppropriateResponse(SendMessageResult sendMessageResult, string clientNonce, string successMessage)
        {
            if (clientNonce != null)
            {
                _httpContext.Response.SetClientNonce(clientNonce);
            }
            if (sendMessageResult.MessageSent)
            {
                return Ok(successMessage);
            }
            else
            {
                return ServerError(sendMessageResult.ErrorMessageForEndUser);
            }
        }

        private async Task<ActionResponse> SignInAndRedirectAsync(SignInMethod method, string username, bool? staySignedIn, string nextUrl, bool? nonceWasValid)
        {
            /*
            Do they have a partial sign in, and this was the second credential (code + password) or (password + code) [= 2 of 3]
                Yes, skip to SIGN IN
            Is this an authorized device? [= 2 of 3]
                Yes, skip to SIGN IN
            Do they have NO authorized devices? [= new account or someone who deleted all authorized devices, 1 of 1|2]
                Yes, skip to SIGN IN
            Is 2FA enabled? (they have a password set and have chosen to require it when authorizing a new device)
                Yes. Do partial sign in and prompt for the relevant second factor (automatically mailed code or password)
            Used password to sign in? [= 1 of 2]
                Yes, skip to SIGN IN
            NonceWasValid [= 1 of 1|2]
                No, back to  sign in screen [prevents someone who passively observing code in transit/storage from using it undetected]
            (local) SIGN IN
            Is this a new device?
                Yes, redirect to AUTHORIZE NEW DEVICE
            Is their account set to prompt them to choose a password
                Yes, redirect to SET PASSWORD
            Is their account set to prompt them to enable 2FA?
                Yes, redirect to ENABLE 2FA
            Has the application they are signing in to (if any) have required claims that have not been set?
                Yes, redirect to COMPLETE PROFILE
            Is their account set to prompt them to see/acknowledge any other screen?
                Yes, redirect SOMEWHERE
            FULL SIGN IN AND REDIRECT back to app (or to default post login page, apps page if none)

            */
            //do 2FA and figure out new device, etc here

            var user = await _userStore.GetUserByEmailAsync(username); //todo: support non-email addresses
            if(user == null)
            {
                // this could only happen if an account was removed, but a method of signing in remained
                return Unauthenticated("Account not found");
            }

            var deviceIsAuthorized = await _authorizedDeviceStore
                .GetAuthorizedDeviceAsync(user.SubjectId, _httpContext.Request.GetDeviceId()) != null;

            if(!deviceIsAuthorized)
            {
                var anyAuthorizedDevices = (await _authorizedDeviceStore.GetAuthorizedDevicesAsync(user.SubjectId))?.Any() ?? false;
                if(anyAuthorizedDevices)
                {
                    // todo: if SecurityLevel == High OR first thing was a password, do a partial sign in and prompt for second thing

                    if((method == SignInMethod.OneTimeCode || method == SignInMethod.Link) && nonceWasValid == false)
                    {
                        return Unauthenticated("Your one time code has expired. Please request a new one.");
                    }
                }
            }

            if (!deviceIsAuthorized && staySignedIn == true)
            {
                // todo: instead of auto approving a device when they check stay signed in, do a partial 
                // login and redirect them to a screen where they can approve the new device
                var description = _httpContext.Request.Headers["User-Agent"];
                var deviceId = await AuthorizeDeviceAsync(user.SubjectId, description);
            }

            await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.SubjectId, user.Email));

            var authProps = (AuthenticationProperties)null;
            if (staySignedIn == true || (method == SignInMethod.Link && deviceIsAuthorized))
            {
                authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(_config.MaxSessionLengthMinutes))
                };
            }

            await _httpContext.SignInAsync(user.SubjectId, user.Email, authProps);

            return Redirect(ValidatedNextUrl(nextUrl));
        }

        public async Task<string> AuthorizeDeviceAsync(string subjectId, string deviceDescription = null)
        {
            var deviceId = _httpContext.Request.GetDeviceId();
            // todo: Review. Accepting an existing device id opens an attack vector for pre-installing
            // cookies on a device or via a malicious browser extension. May want to have a per-user
            // device id that is stored in a DeviceId_[UniqueUserSuffix] cookie
            if (deviceId == null || !(new Regex(@"^[0-9]{10,30}$").IsMatch(deviceId)))
            {
                var rngProvider = new RNGCryptoServiceProvider();
                var byteArray = new byte[8];

                rngProvider.GetBytes(byteArray);
                var deviceIdUInt = BitConverter.ToUInt64(byteArray, 0);
                deviceId = deviceIdUInt.ToString();
            }

            var result = await _authorizedDeviceStore.AddAuthorizedDeviceAsync(subjectId, deviceId, deviceDescription);

            _httpContext.Response.SetDeviceId(deviceId);

            return deviceId;
        }


        private string ValidatedNextUrl(string nextUrl)
        {
            if (_interaction.IsValidReturnUrl(nextUrl) || _urlHelper.IsLocalUrl(nextUrl))
            {
                return nextUrl;
            }
            // todo: get default redirect url from config
            return _urlHelper.Action("Apps", "Home");
        }

        private string SendToSetPasswordFirst(string nextUrl)
        {
            var setPasswordUrl = _urlHelper.Action("SetPassword", "Account");
            return $"{setPasswordUrl}?nextUrl={nextUrl}";
        }

        private async Task<bool> ApplicationIdIsNullOrValidAsync(string applicationId)
        {
            if(applicationId == null)
            {
                return true;
            }
            var app = await _clientStore.FindEnabledClientByIdAsync(applicationId);
            return app != null;
        }
    }
}