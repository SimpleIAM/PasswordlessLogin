// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Serialization;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Entities;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;
using SimpleIAM.OpenIdAuthority.UI.Shared;

namespace SimpleIAM.OpenIdAuthority.API
{
    [Route("api/v1")]
    public class AuthenticateApiController : BaseController
    {
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly ISubjectStore _subjectStore;
        private readonly IClientStore _clientStore;
        //private readonly IPasswordService _passwordService;
        //private readonly IdProviderConfig _config;

        public AuthenticateApiController(
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            ISubjectStore subjectStore,
            IdProviderConfig config,
            IClientStore clientStore,
            IPasswordService passwordService)
        {
            _oneTimeCodeService = oneTimeCodeService;
            _subjectStore = subjectStore;
            _messageService = messageService;
            _clientStore = clientStore;
            //_passwordService = passwordService;
            //_config = config;
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

        private IActionResult Conflict(object obj)
        {
            return CustomResponse(obj, 409);
        }

        private IActionResult ServerError(object obj)
        {
            return CustomResponse(obj, 500);
        }

        private IActionResult CustomResponse(object obj, int statusCode)
        {
            return new JsonResult(obj) { StatusCode = statusCode };
        }        
    }
}