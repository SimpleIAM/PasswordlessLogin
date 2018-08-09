// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Email
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ILogger _logger;
        private readonly IEmailService _emailService;
        private readonly EmailTemplates _templates;

        public EmailTemplateService(ILogger<EmailTemplateService> logger, IEmailService emailService, EmailTemplates templates)
        {
            _logger = logger;
            _templates = templates;
            _emailService = emailService;
        }

        public async Task<SendMessageResult> SendEmailAsync(string templateName, string to, IDictionary<string, string> fields)
        {
            if (_templates.TryGetValue(templateName, out EmailTemplate template))
            {
                _logger.LogDebug("Merging data into email template: ", template);
                var from = new StringBuilder(template.From);
                var subject = new StringBuilder(template.Subject);
                var body = new StringBuilder(template.Body);
                foreach (var field in fields)
                {
                    from = from.Replace("{{" + field.Key + "}}", field.Value);
                    subject = subject.Replace("{{" + field.Key + "}}", field.Value);
                    body = body.Replace("{{" + field.Key + "}}", field.Value);
                }
                return await _emailService.SendEmailAsync(from.ToString(), to, subject.ToString(), body.ToString());
            }
            else
            {
                _logger.LogError("Email template not found: ", template);
                throw new Exception($"Email template '{templateName}' not found"); //todo: create new exception type
            }
        }
    }
}
