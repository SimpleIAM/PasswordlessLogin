// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Email
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private IEmailService _emailService;
        private EmailTemplates _templates;

        public EmailTemplateService(IEmailService emailService, EmailTemplates templates)
        {
            _templates = templates;
            _emailService = emailService;
        }

        public async Task<SendMessageResult> SendEmailAsync(string templateName, string to, Dictionary<string, string> fields)
        {
            if (_templates.TryGetValue(templateName, out EmailTemplate template))
            {
                var subject = new StringBuilder(template.Subject);
                var body = new StringBuilder(template.Body);
                foreach (var field in fields)
                {
                    subject = subject.Replace("{{" + field.Key + "}}", field.Value);
                    body = body.Replace("{{" + field.Key + "}}", field.Value);
                }
                return await _emailService.SendEmailAsync(template.From, to, subject.ToString(), body.ToString());
            }
            else
            {
                throw new Exception($"Email template '{templateName}' not found"); //todo: create new exception type
            }
        }
    }
}
