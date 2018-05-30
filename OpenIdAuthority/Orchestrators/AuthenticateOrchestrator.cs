// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Entities;
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
        private readonly ISubjectStore _subjectStore;
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IPasswordService _passwordService;
        private readonly IdProviderConfig _config;

        public AuthenticateOrchestrator(
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            ISubjectStore subjectStore,
            IdProviderConfig config,
            IClientStore clientStore,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IPasswordService passwordService)
        {
            _oneTimeCodeService = oneTimeCodeService;
            _subjectStore = subjectStore;
            _messageService = messageService;
            _clientStore = clientStore;
            _passwordService = passwordService;
            _interaction = interaction;
            _events = events;
            _config = config;
        }

        public async Task<ActionResponse> Register(RegisterInputModel model)
        {
            if(!string.IsNullOrEmpty(model.ApplicationId)) 
            {
                var app = await _clientStore.FindEnabledClientByIdAsync(model.ApplicationId);
                if(app == null)
                {
                    return BadRequest("Invalid application id");
                }
            }

            var existingSubject = await _subjectStore.GetSubjectByEmailAsync(model.Email);
            if(existingSubject == null) {
                var newSubject = new Subject()
                {
                    Email = model.Email,
                };
                //todo: filter claims and add allowed claims
                newSubject = await _subjectStore.AddSubjectAsync(newSubject);
            }
            else
            {
                //may want allow admins to configure a different email to send to existing users. However, it could be that the user
                // exists but just never got a welcome email...
            }

            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Email, TimeSpan.FromHours(24), model.NextUrl);
            if(oneTimeCodeResponse.Result == GetOneTimeCodeResult.Success)
            {
                var result = await _messageService.SendWelcomeMessageAsync(model.ApplicationId, model.Email, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode, model.MailMergeValues);
                if(result.MessageSent)
                {
                    return Ok("Welcome email sent");
                }
                else
                {
                    return ServerError(result.ErrorMessageForEndUser);
                }
            }
            return BadRequest();
        }
        
        public async Task<ActionResponse> SendOneTimeCode(SendCodeInputModel model)
        {
            // todo: support usernames/phone numbers
            // Note: Need to keep messages generic as to not reveal whether an account exists or not. 
            // If the username provide is not an email address or phone number, tell the user "we sent you a code if you have an account"
            if (model.Username?.Contains("@") == true) // temporary rough email check
            {
                var subject = await _subjectStore.GetSubjectByEmailAsync(model.Username);
                if (subject != null)
                {
                    var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Username, TimeSpan.FromMinutes(5), model.NextUrl);                       
                    switch (oneTimeCodeResponse.Result)
                    {
                        case GetOneTimeCodeResult.Success:
                            var response = await _messageService.SendOneTimeCodeAndLinkMessageAsync(model.Username, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode);
                            if(!response.MessageSent)
                            {
                                var endUserErrorMessage = response.ErrorMessageForEndUser ?? "Hmm, something went wrong. Can you try again?";
                                return ServerError(endUserErrorMessage);
                            }
                            break;
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
                    var result = await _messageService.SendAccountNotFoundMessageAsync(model.Username);
                    if(!result.MessageSent)
                    {
                        return ServerError(result.ErrorMessageForEndUser);
                    }
                }
                return Ok("Message sent. Please check your email.");

            }
            else
            {
                return BadRequest("Please enter a valid email address");
            }
        }

        public async Task<ActionResponse> Authenticate(AuthenticateInputModel model)
        {
            model.OneTimeCode = model.OneTimeCode.Replace(" ", "");
            var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(model.Username, model.OneTimeCode);
            switch (response.Result)
            {
                case CheckOneTimeCodeResult.Verified:
                    var subject = await _subjectStore.GetSubjectByEmailAsync(model.Username); //todo: handle non-email addresses
                    if(subject != null)
                    {
                        return Ok(response.RedirectUrl);
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

        public async Task<ActionResponse> AuthenticatePassword(AuthenticatePasswordInputModel model)
        {
            var subject = await _subjectStore.GetSubjectByEmailAsync(model.Username); //todo: handle non-email addresses
            if(subject == null)
            {
                return Unauthenticated("The email address or password wasn't right");
            }
            else
            {
                var checkPasswordResult = await _passwordService.CheckPasswordAsync(subject.SubjectId, model.Password);
                switch (checkPasswordResult)
                {
                    case CheckPasswordResult.NotFound:
                    case CheckPasswordResult.PasswordIncorrect:
                        return Unauthenticated("The email address or password wasn't right");
                    case CheckPasswordResult.TemporarilyLocked:
                        return Unauthenticated("Your password is temporarily locked. Use a one time code to sign in.");
                    case CheckPasswordResult.Success:
                        return Ok();
                    case CheckPasswordResult.ServiceFailure:
                    default:
                        return ServerError("Hmm. Something went wrong. Please try again.");
                }
            }
        }

        public async Task<ActionResponse> SendPasswordResetMessage(SendPasswordResetMessageInputModel model)
        {
            if(!string.IsNullOrEmpty(model.ApplicationId)) 
            {
                var app = await _clientStore.FindEnabledClientByIdAsync(model.ApplicationId);
                if(app == null)
                {
                    return BadRequest("Invalid application id");
                }
            }

            var subject = await _subjectStore.GetSubjectByEmailAsync(model.Username); //todo: support non-email addresses
            if(subject == null) 
            {
                // if valid email or phone number, send a message inviting them to register
                if(model.Username.Contains("@")) {
                    var result = await _messageService.SendAccountNotFoundMessageAsync(model.Username);
                    if(!result.MessageSent)
                    {
                        return ServerError(result.ErrorMessageForEndUser);
                    }
                }
                return Ok("Check your email for password reset instructions.");
            }

            var nextUrl = string.IsNullOrEmpty(model.NextUrl) ? "/account/setpassword?nextUrl=/apps" : "/account/setpassword?nextUrl=" + model.NextUrl;
            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Username, TimeSpan.FromMinutes(5), nextUrl);
            if(oneTimeCodeResponse.Result == GetOneTimeCodeResult.Success)
            {
                var result = await _messageService.SendPasswordResetMessageAsync(model.ApplicationId, model.Username, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode);
                if(result.MessageSent)
                {
                    return Ok("Check your email for password reset instructions.");
                }
                else
                {
                    return ServerError(result.ErrorMessageForEndUser);
                }
            }
            return BadRequest();
        }

        public async Task<string> SignInUserAndGetNextUrl(HttpContext httpContext, string username, bool staySignedIn, string returnUrl)
        {
            var subject = await _subjectStore.GetSubjectByEmailAsync(username); //todo: support non-email addresses

            await _events.RaiseAsync(new UserLoginSuccessEvent(subject.Email, subject.SubjectId, subject.Email));

            var authProps = (AuthenticationProperties)null;
            if(staySignedIn) {
                authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(_config.MaxSessionLengthMinutes))
                };
            }
        
            await httpContext.SignInAsync(subject.SubjectId, subject.Email, authProps);

            if (_interaction.IsValidReturnUrl(returnUrl))
            {
                return returnUrl;
            }
            return "/apps";
        }
    }
}