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

namespace SimpleIAM.OpenIdAuthority.API.AuthenticateApi
{
    [Route("api/v1")]
    public class AuthenticateApiController : BaseController
    {
        //private readonly IIdentityServerInteractionService _interaction;
        //private readonly IEventService _events;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly ISubjectStore _subjectStore;
        //private readonly IClientStore _clientStore;
        //private readonly IPasswordService _passwordService;
        //private readonly IdProviderConfig _config;

        public AuthenticateApiController(
            IIdentityServerInteractionService interaction,
            IEventService events,
            IOneTimeCodeService oneTimeCodeService,
            ISubjectStore subjectStore,
            IdProviderConfig config,
            IClientStore clientStore,
            IPasswordService passwordService)
        {
            //_interaction = interaction;
            //_events = events;
            _oneTimeCodeService = oneTimeCodeService;
            _subjectStore = subjectStore;
            //_clientStore = clientStore;
            //_passwordService = passwordService;
            //_config = config;
        }

        [HttpPost("send-code")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeInputModel model)
        {
            if (ModelState.IsValid)
            {
                // todo: support usernames/phone numbers
                // Note: Need to keep messages generic as to not reveal whether an account exists or not. 
                // If the username provide is not an email address or phone number, tell the user that we sent them a code IF they have an account
                if (model.Username?.Contains("@") == true) // temporary rough email check
                {
                    var subject = await _subjectStore.GetSubjectByEmailAsync(model.Username);
                    if (subject != null)
                    {
                        var response = await _oneTimeCodeService.SendOneTimeCodeAndLinkAsync(model.Username, TimeSpan.FromMinutes(5), model.ContinueUrl);
                        switch (response.Result)
                        {
                            case SendOneTimeCodeResult.Sent:
                                break;
                            case SendOneTimeCodeResult.TooManyRequests:
                                BadRequest("Please wait a few minutes before requesting a new code");
                                break;
                            case SendOneTimeCodeResult.InvalidRequest:
                            case SendOneTimeCodeResult.ServiceFailure:
                            default:
                                var endUserErrorMessage = response.MessageForEndUser ?? "Hmm, something went wrong. Can you try again?";
                                return ServerError(endUserErrorMessage);
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
                    BadRequest("Please enter a valid email address");
                }
            }
            return BadRequest();
        }

        private IActionResult ServerError(string messageForEndUser)
        {
            var serializeSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            serializeSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy(),
            };
            return new JsonResult(new { MessageForEndUser = messageForEndUser }, serializeSettings) { StatusCode = 500 };
        }        
    }
}