// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class AuthenticateOrchestrator : ActionResponder
    {
        private enum SignInMethod
        {
            Password,
            OneTimeCode,
            Link
        }
        private readonly ILogger _logger;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;
        private readonly IdProviderConfig _config;
        private readonly IUrlService _urlService;
        private readonly HttpContext _httpContext;
        private readonly IAuthorizedDeviceStore _authorizedDeviceStore;
        private readonly ISignInService _signInService;
        private readonly IApplicationService _applicationService;

        public AuthenticateOrchestrator(
            ILogger<AuthenticateOrchestrator> logger,
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            IUserStore userStore,
            IdProviderConfig config,
            IPasswordService passwordService,
            IUrlService urlService,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizedDeviceStore authorizedDeviceStore,
            ISignInService signInService,
            IApplicationService applicationService)
        {
            _logger = logger;
            _oneTimeCodeService = oneTimeCodeService;
            _userStore = userStore;
            _messageService = messageService;
            _passwordService = passwordService;
            _config = config;
            _urlService = urlService;
            _httpContext = httpContextAccessor.HttpContext;
            _authorizedDeviceStore = authorizedDeviceStore;
            _signInService = signInService;
            _applicationService = applicationService;
        }

        public async Task<ActionResponse> RegisterAsync(RegisterInputModel model)
        {
            _logger.LogDebug("Begin registration for {0}", model.Email);

            if (!ApplicationIdIsNullOrValid(model.ApplicationId))
            {
                return BadRequest("Invalid application id");
            }

            TimeSpan linkValidity;
            if (await _userStore.UsernameIsAvailable(model.Email))
            {
                if(await _oneTimeCodeService.UnexpiredOneTimeCodeExistsAsync(model.Email))
                {
                    // alternatively, we could send an email message explaining that the recently freed up email
                    // address can't be linked to a new account yet (must wait until the link that can cancel
                    // the username change expires)
                    return BadRequest("Email address is temporarily reserved");
                }
                _logger.LogDebug("Email address not used by an existing user. Creating a new user.");
                // todo: consider restricting claims to a list predefined by the system administrator
                var newUser = new User()
                {
                    Email = model.Email,
                    Claims = model.Claims?
                        .Where(x => 
                            !PasswordlessLoginConstants.Security.ForbiddenClaims.Contains(x.Key) && 
                            !PasswordlessLoginConstants.Security.ProtectedClaims.Contains(x.Key))
                        .Select(x => new UserClaim() { Type = x.Key, Value = x.Value })
                };
                newUser = await _userStore.AddUserAsync(newUser);
                linkValidity = TimeSpan.FromMinutes(PasswordlessLoginConstants.OneTimeCode.ConfirmAccountDefaultValidityMinutes);
            }
            else
            {
                _logger.LogDebug("Existing user found.");
                linkValidity = TimeSpan.FromMinutes(PasswordlessLoginConstants.OneTimeCode.DefaultValidityMinutes);
                //may want allow admins to configure a different email to send to existing users. However, it could be that the user
                // exists but just never got a welcome email?
            }

            var nextUrl = !string.IsNullOrEmpty(model.NextUrl) ? model.NextUrl : _urlService.GetDefaultRedirectUrl();
            if (model.SetPassword)
            {
                _logger.LogTrace("The user will be asked to set their password after confirming the account.");
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
            _logger.LogDebug("Begin send one time code for {0}", model.Username);

            if (!ApplicationIdIsNullOrValid(model.ApplicationId))
            {
                return BadRequest("Invalid application id");
            }

            // todo: support usernames/phone numbers
            // Note: Need to keep messages generic as to not reveal whether an account exists or not. 
            // If the username provide is not an email address or phone number, tell the user "we sent you a code if you have an account"
            if (model.Username?.Contains("@") == true) // temporary rough email check
            {
                if (await _userStore.UserExists(model.Username))
                {
                    _logger.LogDebug("User found");
                    //todo: get validity timespan from config
                    var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(
                        model.Username, 
                        TimeSpan.FromMinutes(PasswordlessLoginConstants.OneTimeCode.DefaultValidityMinutes), 
                        model.NextUrl);
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
                    _logger.LogDebug("User not found");
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
                _logger.LogError("{0} is not a valid email address", model.Username);
                return BadRequest("Please enter a valid email address");
            }
        }

        public async Task<ActionResponse> AuthenticateAsync(AuthenticatePasswordInputModel model)
        {
            _logger.LogDebug("Begin authentication for {0}", model.Username);

            var oneTimeCode = model.Password.Replace(" ", "");
            if (oneTimeCode.Length == PasswordlessLoginConstants.OneTimeCode.ShortCodeLength && oneTimeCode.All(Char.IsDigit))
            {
                _logger.LogDebug("Password was a six-digit number");
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
                _logger.LogDebug("Password was not a six-digit number");
                return await AuthenticatePasswordAsync(model);
            }
        }

        public async Task<ActionResponse> AuthenticateCodeAsync(AuthenticateInputModel model)
        {
            _logger.LogDebug("Begin one time code authentication for {0}", model.Username);

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
            _logger.LogDebug("Begin password authentication for {0}", model.Username);

            var user = await _userStore.GetUserByEmailAsync(model.Username); //todo: handle non-email addresses
            if (user == null)
            {
                _logger.LogDebug("User not found: {0}", model.Username);
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
            _logger.LogDebug("Begin long code (one time link) authentication");

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
                    return NotFound();
                case CheckOneTimeCodeResult.NotFound:
                    return Unauthenticated("The sign in link is invalid.");
                case CheckOneTimeCodeResult.ServiceFailure:
                default:
                    return ServerError("Something went wrong.");
            }
        }

        public async Task<ActionResponse> SendPasswordResetMessageAsync(SendPasswordResetMessageInputModel model)
        {
            _logger.LogDebug("Begin send password reset message for {0}", model.Username);

            if (!ApplicationIdIsNullOrValid(model.ApplicationId))
            {
                return BadRequest("Invalid application id");
            }

            if (!await _userStore.UserExists(model.Username))
            {
                _logger.LogInformation("User not found: {0}", model.Username);
                // if valid email or phone number, send a message inviting them to register
                if (model.Username.Contains("@"))
                {
                    var result = await _messageService.SendAccountNotFoundMessageAsync(model.ApplicationId, model.Username);
                    if (!result.MessageSent)
                    {
                        return ServerError(result.ErrorMessageForEndUser);
                    }
                }
                else
                {
                    _logger.LogInformation("Account not found message was not sent because provided username is not an email address: {0}", model.Username);
                }
                return Ok("Check your email for password reset instructions.");
            }
            var nextUrl = SendToSetPasswordFirst(!string.IsNullOrEmpty(model.NextUrl) ? model.NextUrl : _urlService.GetDefaultRedirectUrl());
            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(
                model.Username, 
                TimeSpan.FromMinutes(PasswordlessLoginConstants.OneTimeCode.DefaultValidityMinutes), 
                nextUrl);
            if (oneTimeCodeResponse.Result == GetOneTimeCodeResult.Success)
            {
                var result = await _messageService.SendPasswordResetMessageAsync(model.ApplicationId, model.Username, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode);
                return ReturnAppropriateResponse(result, oneTimeCodeResponse.ClientNonce, "Check your email for password reset instructions.");
            }
            _logger.LogError("Password reset message was not be sent due to error encountered while generating a one time link");
            return ServerError("Hmm. Something went wrong. Please try again.");
        }

        private ActionResponse ReturnAppropriateResponse(SendMessageResult sendMessageResult, string clientNonce, string successMessage)
        {
            if (clientNonce != null)
            {
                _logger.LogDebug("Saving client nonce in a browser cookie");
                _httpContext.Response.SetClientNonce(clientNonce);
            }
            if (sendMessageResult.MessageSent)
            {
                return Ok(successMessage);
            }
            else
            {
                _logger.LogDebug("Returning error message to user: {0}", sendMessageResult.ErrorMessageForEndUser);
                return ServerError(sendMessageResult.ErrorMessageForEndUser);
            }
        }

        private async Task<ActionResponse> SignInAndRedirectAsync(SignInMethod method, string username, bool? staySignedIn, string nextUrl, bool? nonceWasValid)
        {
            _logger.LogTrace("Begining sign in and redirect logic for {0}", username);
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
                _logger.LogError("Strangely, there is no account for {0} anymore", username);
                return Unauthenticated("Account not found");
            }

            var deviceIsAuthorized = await _authorizedDeviceStore
                .GetAuthorizedDeviceAsync(user.SubjectId, _httpContext.Request.GetDeviceId()) != null;

            if(!deviceIsAuthorized)
            {
                _logger.LogDebug("Device user is signing in from is not authorized");
                var anyAuthorizedDevices = (await _authorizedDeviceStore.GetAuthorizedDevicesAsync(user.SubjectId))?.Any() ?? false;
                if(anyAuthorizedDevices)
                {
                    _logger.LogDebug("User does have other devices that are authorized");
                    // todo: if SecurityLevel == High OR first thing was a password, do a partial sign in and prompt for second thing

                    if ((method == SignInMethod.OneTimeCode || method == SignInMethod.Link) && nonceWasValid == false)
                    {
                        _logger.LogWarning("Client nonce was missing or invalid. Perhaps the one time code has " +
                            "been intercepted and an unathorized party is trying to user it. Authentication blocked.");
                        return Unauthenticated("Your one time code has expired. Please request a new one.");
                    }
                }
                else
                {
                    _logger.LogDebug("User does not have any devices that are authorized");
                }
            }

            if (!deviceIsAuthorized && staySignedIn == true)
            {
                // todo: instead of auto approving a device when they check stay signed in, do a partial 
                // login and redirect them to a screen where they can approve the new device
                var description = _httpContext.Request.Headers["User-Agent"];
                var deviceId = await AuthorizeDeviceAsync(user.SubjectId, description);
            }

            var authProps = (AuthenticationProperties)null;
            if (staySignedIn == true || (method == SignInMethod.Link && deviceIsAuthorized))
            {
                _logger.LogTrace("Using maximum session length of {0} minutes", _config.MaxSessionLengthMinutes);
                authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(_config.MaxSessionLengthMinutes))
                };
            }
            else
            {
                _logger.LogTrace("Using default session length of {0} minutes", _config.DefaultSessionLengthMinutes);
            }

            _logger.LogDebug("Signing user in: {0}", user.Email);
            _logger.LogTrace("SubjectId: {0}", user.SubjectId);
            await _signInService.SignInAsync(user.SubjectId, user.Email, authProps);

            nextUrl = ValidatedNextUrl(nextUrl);
            _logger.LogDebug("Redirecting user to: {0}", nextUrl);
            return Redirect(nextUrl);
        }

        public async Task<string> AuthorizeDeviceAsync(string subjectId, string deviceDescription = null)
        {
            _logger.LogDebug("Registering current device as a trusted device");
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
            if (_urlService.IsAllowedRedirectUrl(nextUrl))
            {
                return nextUrl;
            }
            _logger.LogWarning("Next url was not valid: '{0}'. Using default redirect url instead.", nextUrl);
            // todo: get default redirect url from config
            return _urlService.GetDefaultRedirectUrl();
        }

        private string SendToSetPasswordFirst(string nextUrl)
        {
            var setPasswordUrl = _urlService.GetSetPasswordUrl();
            return $"{setPasswordUrl}?nextUrl={nextUrl}";
        }

        private bool ApplicationIdIsNullOrValid(string applicationId)
        {
            if(applicationId == null)
            {
                return true;
            }
            if (!_applicationService.ApplicationExists(applicationId))
            {
                _logger.LogError("Invalid application id '{0}'", applicationId);
                return false;
            }
            return true;
        }
    }
}