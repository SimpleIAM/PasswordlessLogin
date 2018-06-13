// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
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
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly IUserStore _userStore;
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IPasswordService _passwordService;
        private readonly IdProviderConfig _config;
        private readonly IUrlHelper _urlHelper;

        public AuthenticateOrchestrator(
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            IUserStore userStore,
            IdProviderConfig config,
            IClientStore clientStore,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IPasswordService passwordService,
            IUrlHelper urlHelper)
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

        public async Task<ActionResponse> AuthenticateAsync(AuthenticatePasswordInputModel model, string deviceId, string clientNonce)
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
                return await AuthenticateCodeAsync(input, deviceId, clientNonce);
            }
            else
            {
                return await AuthenticatePasswordAsync(model, deviceId);
            }
        }

        public async Task<ActionResponse> AuthenticateCodeAsync(AuthenticateInputModel model, string deviceId, string clientNonce)
        {
            model.OneTimeCode = model.OneTimeCode.Replace(" ", "");
            var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(model.Username, model.OneTimeCode, deviceId, clientNonce);
            switch (response.Result)
            {
                case CheckOneTimeCodeResult.VerifiedOnNewDevice:
                case CheckOneTimeCodeResult.VerifiedOnAuthorizedDevice:
                    var user = await _userStore.GetUserByEmailAsync(model.Username); //todo: handle non-email addresses
                    if (user != null)
                    {
                        return SaveDeviceIdAndRedirect(response);
                    }
                    return Unauthenticated("Invalid one time code");
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

        public async Task<ActionResponse> AuthenticatePasswordAsync(AuthenticatePasswordInputModel model, string deviceId)
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
                        return Redirect(ValidatedNextUrl(model.NextUrl));
                    case CheckPasswordResult.ServiceFailure:
                    default:
                        return ServerError("Hmm. Something went wrong. Please try again.");
                }
            }
        }

        public async Task<ActionResponse> AuthenticateLongCodeAsync(string longCode, string deviceId, string clientNonce)
        {
            if (longCode != null && longCode.Length < 36)
            {
                var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(longCode, deviceId, clientNonce);
                switch (response.Result)
                {
                    case CheckOneTimeCodeResult.VerifiedOnAuthorizedDevice:
                    case CheckOneTimeCodeResult.VerifiedOnNewDevice:
                        return SaveDeviceIdAndRedirect(response);
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

        public async Task SignInUserAsync(HttpContext httpContext, string username, bool staySignedIn)
        {
            var user = await _userStore.GetUserByEmailAsync(username); //todo: support non-email addresses

            await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.SubjectId, user.Email));

            var authProps = (AuthenticationProperties)null;
            if (staySignedIn)
            {
                authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(_config.MaxSessionLengthMinutes))
                };
            }

            await httpContext.SignInAsync(user.SubjectId, user.Email, authProps);
        }

        private ActionResponse ReturnAppropriateResponse(SendMessageResult sendMessageResult, string clientNonce, string successMessage)
        {
            ActionResponse actionResponse;
            if (sendMessageResult.MessageSent)
            {
                actionResponse = Ok(successMessage);
            }
            else
            {
                actionResponse = ServerError(sendMessageResult.ErrorMessageForEndUser);
            }

            if (clientNonce != null)
            {
                actionResponse.Content = new SetClientNonceCommand() { ClientNonce = clientNonce };
            }
            return actionResponse;
        }

        private ActionResponse SaveDeviceIdAndRedirect(CheckOneTimeCodeResponse response)
        {
            // Returning username via Message field (it is needed when validating a long code)
            var actionResponse = new ActionResponse(response.SentTo, 301) { RedirectUrl = ValidatedNextUrl(response.RedirectUrl) };
            if (response.Result == CheckOneTimeCodeResult.VerifiedOnNewDevice)
            {
                actionResponse.Content = new SetDeviceIdCommand();
            }
            return actionResponse;
        }

        private string ValidatedNextUrl(string nextUrl)
        {
            if (_interaction.IsValidReturnUrl(nextUrl) || _urlHelper.IsLocalUrl(nextUrl))
            {
                return nextUrl;
            }
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