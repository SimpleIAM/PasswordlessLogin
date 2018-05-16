using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Services.Email;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Message
{
    public class MessageService : IMessageService
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IUrlHelper _urlHelper;
        private readonly IHttpContextAccessor _httpContext;

        public MessageService(
            IEmailTemplateService emailTemplateService,
            IUrlHelper urlHelper,
            IHttpContextAccessor httpContext
            )
        {
            _emailTemplateService = emailTemplateService;
            _urlHelper = urlHelper;
            _httpContext = httpContext;
        }

        public async Task<SendMessageResult> SendAccountNotFoundMessageAsync(string sendTo)
        {
            if (!IsValidEmailAddress(sendTo)) {
                return NotAnEmailAddress();
            }

            var link = _urlHelper.Action("Register", "Authenticate", new { }, _httpContext.HttpContext.Request.Scheme);
            var fields = new Dictionary<string, string>()
            {
                { "register_link", link },
            };
            return await _emailTemplateService.SendEmailAsync("AccountNotFound", sendTo, fields);
        }

        public async Task<SendMessageResult> SendOneTimeCodeMessageAsync(string sendTo, string oneTimeCode)
        {
            return await SendOneTimeCodeMessageInternalAsync("OneTimeCode", sendTo, oneTimeCode, "");
        }

        public async Task<SendMessageResult> SendOneTimeCodeAndLinkMessageAsync(string sendTo, string oneTimeCode, string longCode)
        {
            return await SendOneTimeCodeMessageInternalAsync("SignInWithEmail", sendTo, oneTimeCode, longCode);
        }

        private async Task<SendMessageResult> SendOneTimeCodeMessageInternalAsync(string template, string sendTo, string oneTimeCode, string longCode)
        {
            if (!IsValidEmailAddress(sendTo))
            {
                return NotAnEmailAddress();
            }

            var link = _urlHelper.Action("SignInLink", "Authenticate", new { longCode = longCode.ToString() }, _httpContext.HttpContext.Request.Scheme);
            var fields = new Dictionary<string, string>()
                {
                    { "one_time_code", oneTimeCode },
                    { "sign_in_link", link },
                };
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
    }
}
