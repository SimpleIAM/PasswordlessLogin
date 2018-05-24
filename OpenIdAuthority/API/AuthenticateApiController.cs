// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Entities;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;

namespace SimpleIAM.OpenIdAuthority.API
{
    [Route("api/v1")]
    public class AuthenticateApiController : BaseApiController
    {
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly ISubjectStore _subjectStore;
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IPasswordService _passwordService;
        private readonly IdProviderConfig _config;

        public AuthenticateApiController(
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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]RegisterInputModel model)
        {
            if (ModelState.IsValid)
            {
                if(!string.IsNullOrEmpty(model.ApplicationId)) 
                {
                    var app = await _clientStore.FindEnabledClientByIdAsync(model.ApplicationId);
                    if(app == null)
                    {
                        return BadRequest(new ApiResponse("Invalid application id"));
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
                        return Ok(new ApiResponse("Welcome email sent"));
                    }
                    else
                    {
                        return ServerError(new ApiResponse(result.ErrorMessageForEndUser));
                    }
                }
            }
            return BadRequest(new ApiResponse(ModelState));
        }
        
        [HttpPost("send-one-time-code")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOneTimeCode([FromBody] SendCodeInputModel model)
        {
            if (ModelState.IsValid)
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
                                    return ServerError(new ApiResponse(endUserErrorMessage));
                                }
                                break;
                            case GetOneTimeCodeResult.TooManyRequests:
                                return BadRequest(new ApiResponse("Please wait a few minutes before requesting a new code"));
                            case GetOneTimeCodeResult.ServiceFailure:
                            default:
                                return ServerError(new ApiResponse("Hmm, something went wrong. Can you try again?"));
                        }
                    }
                    else
                    {
                        // if valid email or phone number, send a message inviting them to register
                        var result = await _messageService.SendAccountNotFoundMessageAsync(model.Username);
                        if(!result.MessageSent)
                        {
                            return ServerError(new ApiResponse(result.ErrorMessageForEndUser));
                        }
                    }
                    return Ok(new ApiResponse("Message sent. Please check your email."));

                }
                else
                {
                    BadRequest(new ApiResponse("Please enter a valid email address"));
                }
            }
            return BadRequest(new ApiResponse(ModelState));
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateInputModel model)
        {
            if (ModelState.IsValid)
            {
                model.OneTimeCode = model.OneTimeCode.Replace(" ", "");
                var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(model.Username, model.OneTimeCode);
                switch (response.Result)
                {
                    case CheckOneTimeCodeResult.Verified:
                        var subject = await _subjectStore.GetSubjectByEmailAsync(model.Username); //todo: handle non-email addresses
                        if(subject != null)
                        {
                            var returnVal = new AuthenticateApiResponse()
                            {
                                NextUrl = await FinishSignIn(subject, model.StaySignedIn, response.RedirectUrl)
                            };
                            return Ok(returnVal);                            
                        }
                        return AuthenticationFailed(new ApiResponse("Invalid one time code"));
                    case CheckOneTimeCodeResult.Expired:
                        return AuthenticationFailed(new ApiResponse("Your one time code has expired. Please request a new one."));
                    case CheckOneTimeCodeResult.CodeIncorrect:
                    case CheckOneTimeCodeResult.NotFound:
                        return AuthenticationFailed(new ApiResponse("Invalid one time code"));
                    case CheckOneTimeCodeResult.ShortCodeLocked:
                        return AuthenticationFailed(new ApiResponse("The one time code is locked. Please request a new one after a few minutes."));
                    case CheckOneTimeCodeResult.ServiceFailure:
                    default:
                        return ServerError(new ApiResponse("Something went wrong."));
                }
            }
            return BadRequest(new ApiResponse(ModelState));
        }

        //todo: don't respond to cross-origin request
        [HttpPost("authenticate-password")]
        public async Task<IActionResult> AuthenticatePassword([FromBody] AuthenticatePasswordInputModel model)
        {
            if (ModelState.IsValid)
            {
                var subject = await _subjectStore.GetSubjectByEmailAsync(model.Username); //todo: handle non-email addresses
                if(subject == null)
                {
                    return AuthenticationFailed(new ApiResponse("The email address or password wasn't right"));
                }
                else
                {
                    var checkPasswordResult = await _passwordService.CheckPasswordAsync(subject.SubjectId, model.Password);
                    switch (checkPasswordResult)
                    {
                        case CheckPasswordResult.NotFound:
                        case CheckPasswordResult.PasswordIncorrect:
                            return AuthenticationFailed(new ApiResponse("The email address or password wasn't right"));
                        case CheckPasswordResult.TemporarilyLocked:
                            return AuthenticationFailed(new ApiResponse("Your password is temporarily locked. Use a one time code to sign in."));
                        case CheckPasswordResult.ServiceFailure:
                            return ServerError(new ApiResponse("Hmm. Something went wrong. Please try again."));
                        case CheckPasswordResult.Success:
                            var returnVal = new AuthenticateApiResponse()
                            {
                                NextUrl = await FinishSignIn(subject, model.StaySignedIn, model.NextUrl)
                            };
                            return Ok(returnVal);                            
                    }
                }

            }
            return BadRequest(new ApiResponse(ModelState));
        }

        [HttpPost("send-password-reset-message")]
        public async Task<IActionResult> SendPasswordResetMessage([FromBody]SendPasswordResetMessageInputModel model)
        {
            if (ModelState.IsValid)
            {
                if(!string.IsNullOrEmpty(model.ApplicationId)) 
                {
                    var app = await _clientStore.FindEnabledClientByIdAsync(model.ApplicationId);
                    if(app == null)
                    {
                        return BadRequest(new ApiResponse("Invalid application id"));
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
                            return ServerError(new ApiResponse(result.ErrorMessageForEndUser));
                        }
                    }
                    return Ok(new ApiResponse("Check your email for password reset instructions."));
                }

                var nextUrl = string.IsNullOrEmpty(model.NextUrl) ? "/account/setpassword?nextUrl=/apps" : "/account/setpassword?nextUrl=" + model.NextUrl;
                var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Username, TimeSpan.FromMinutes(5), nextUrl);
                if(oneTimeCodeResponse.Result == GetOneTimeCodeResult.Success)
                {
                    var result = await _messageService.SendPasswordResetMessageAsync(model.ApplicationId, model.Username, oneTimeCodeResponse.ShortCode, oneTimeCodeResponse.LongCode);
                    if(result.MessageSent)
                    {
                        return Ok(new ApiResponse("Check your email for password reset instructions."));
                    }
                    else
                    {
                        return ServerError(new ApiResponse(result.ErrorMessageForEndUser));
                    }
                }
            }
            return BadRequest(new ApiResponse(ModelState));
        }

        private async Task<string> FinishSignIn(Subject subject, bool staySignedIn, string returnUrl)
        {
            await _events.RaiseAsync(new UserLoginSuccessEvent(subject.Email, subject.SubjectId, subject.Email));

            var authProps = (AuthenticationProperties)null;
            if(staySignedIn) {
                authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(_config.MaxSessionLengthMinutes))
                };
            }
        
            await HttpContext.SignInAsync(subject.SubjectId, subject.Email, authProps);

            if (_interaction.IsValidReturnUrl(returnUrl))
            {
                return returnUrl;
            }
            return "/apps";
        }
    }
}