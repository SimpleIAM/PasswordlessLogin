// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Services.Email;
using SimpleIAM.OpenIdAuthority.UI.Authenticate;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Message
{
    public class MessageService : IMessageService
    {
        private readonly ILogger _logger;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IUrlHelper _urlHelper;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IdProviderConfig _idProviderConfig;
        private readonly IClientStore _clientStore;

        public MessageService(
            ILogger<MessageService> logger,
            IEmailTemplateService emailTemplateService,
            IUrlHelper urlHelper,
            IHttpContextAccessor httpContext,
            IdProviderConfig idProviderConfig,
            IClientStore clientStore
            )
        {
            _logger = logger;
            _emailTemplateService = emailTemplateService;
            _urlHelper = urlHelper;
            _httpContext = httpContext;
            _idProviderConfig = idProviderConfig;
            _clientStore = clientStore;
        }

        public async Task<SendMessageResult> SendAccountNotFoundMessageAsync(string applicationId, string sendTo)
        {
            _logger.LogDebug("Sending account not found message");
            if (!IsValidEmailAddress(sendTo)) {
                return NotAnEmailAddress();
            }

            var link = GenerateUrl(nameof(AuthenticateController.Register));
            var fields = await GetCustomFieldsAsync(applicationId);
            fields["register_link"] = link;
            return await _emailTemplateService.SendEmailAsync(OpenIdAuthorityConstants.EmailTemplates.AccountNotFound, sendTo, fields);
        }

        public async Task<SendMessageResult> SendOneTimeCodeMessageAsync(string applicationId, string sendTo, string oneTimeCode)
        {
            _logger.LogDebug("Sending one time code message");
            return await SendOneTimeCodeMessageInternalAsync(OpenIdAuthorityConstants.EmailTemplates.OneTimeCode, applicationId, sendTo, oneTimeCode, "");
        }

        public async Task<SendMessageResult> SendOneTimeCodeAndLinkMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode)
        {
            _logger.LogDebug("Sending one time code and link message");
            return await SendOneTimeCodeMessageInternalAsync(OpenIdAuthorityConstants.EmailTemplates.SignInWithEmail, applicationId, sendTo, oneTimeCode, longCode);
        }

        private async Task<SendMessageResult> SendOneTimeCodeMessageInternalAsync(string template, string clientId, string sendTo, string oneTimeCode, string longCode)
        {
            if (!IsValidEmailAddress(sendTo))
            {
                return NotAnEmailAddress();
            }

            var link = GenerateUrl(nameof(AuthenticateController.SignInLink), new { longCode = longCode.ToString() });
            var fields = await GetCustomFieldsAsync(clientId);
            fields["one_time_code"] = oneTimeCode;
            fields["sign_in_link"] = link;
            return await _emailTemplateService.SendEmailAsync(template, sendTo, fields);
        }

        private bool IsValidEmailAddress(string sendTo)
        {
            return sendTo?.Contains("@") == true; // todo: have a better email check
        }

        private SendMessageResult NotAnEmailAddress()
        {
            return SendMessageResult.Failed("Could not deliver message. Email address is not valid."); // non-email addresses not implemented
        }

        public async Task<SendMessageResult> SendWelcomeMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode, IDictionary<string, string> userFields)
        {
            _logger.LogDebug("Sending welcome message");
            if (!IsValidEmailAddress(sendTo))
            {
                return NotAnEmailAddress();
            }

            var link = GenerateUrl(nameof(AuthenticateController.SignInLink), new { longCode = longCode.ToString() });
            var signInUrl = GenerateUrl(nameof(AuthenticateController.SignIn));
            var fields = await GetCustomFieldsAsync(applicationId);
            if(userFields != null)
            {
                foreach (var field in userFields)
                {
                    if (!fields.ContainsKey(field.Key))
                    {
                        fields[field.Key] = field.Value;
                    }
                }
            }
            fields["one_time_code"] = oneTimeCode;
            fields["sign_in_link"] = link;
            fields["sign_in_url"] = signInUrl;

            return await _emailTemplateService.SendEmailAsync(OpenIdAuthorityConstants.EmailTemplates.Welcome, sendTo, fields);
        }

        public async Task<SendMessageResult> SendPasswordResetMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode)
        {
            _logger.LogDebug("Sending password reset message");
            if (!IsValidEmailAddress(sendTo))
            {
                return NotAnEmailAddress();
            }

            var link = GenerateUrl(nameof(AuthenticateController.SignInLink), new { longCode = longCode.ToString() });
            var signInUrl = GenerateUrl(nameof(AuthenticateController.SignIn));
            var fields = await GetCustomFieldsAsync(applicationId);
            fields["one_time_code"] = oneTimeCode;
            fields["password_reset_link"] = link;

            return await _emailTemplateService.SendEmailAsync(OpenIdAuthorityConstants.EmailTemplates.PasswordReset, sendTo, fields);
        }

        protected async Task<IDictionary<string, string>> GetCustomFieldsAsync(string applicationId)
        {
            var fields = new Dictionary<string, string>(_idProviderConfig.CustomProperties);
            if (applicationId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(applicationId);
                if(client != null && client.Properties != null)
                {
                    foreach(var field in client.Properties)
                    {
                        fields[field.Key] = field.Value;
                    }
                }
            }
            return fields;
        }

        protected string GenerateUrl(string action, object values = null)
        {
            var controllerName = typeof(AuthenticateController).Name;
            controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);
            return _urlHelper.Action(action, controllerName, values ?? new { }, _httpContext.HttpContext.Request.Scheme);
        }
    }
}
