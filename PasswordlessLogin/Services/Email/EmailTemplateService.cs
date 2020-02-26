// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using StandardResponse;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Email
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ILogger _logger;
        private readonly IEmailService _emailService;
        private readonly EmailTemplates _templates;
        private readonly PasswordlessLoginOptions _options;

        public EmailTemplateService(
            ILogger<EmailTemplateService> logger, 
            IEmailService emailService, 
            EmailTemplates templates,
            PasswordlessLoginOptions options)
        {
            _logger = logger;
            _templates = templates;
            _emailService = emailService;
            _options = options;
        }

        public async Task<Status> SendEmailAsync(string templateName, string to, IDictionary<string, string> fields)
        {
            EmailTemplate template;
            var cultureCode = CultureInfo.CurrentCulture.Name;
            var languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName; // The 2-letter code if it exists, otherwise a 3-letter code
            if (!(_templates.TryGetValue($"{templateName}.{cultureCode}", out template)
                ||_templates.TryGetValue($"{templateName}.{languageCode}", out template)
                || _templates.TryGetValue(templateName, out template)))
            {
                _logger.LogError("Email template not found: ", template);
                throw new Exception($"Email template '{templateName}' not found"); //todo: create new exception type
            }

            _logger.LogDebug("Merging data into email template: ", template);
            var from = new StringBuilder(_options.EmailFrom);
            var subject = new StringBuilder(template.Subject);
            var body = new StringBuilder(template.Body);
            if(fields != null)
            {
                foreach (var field in fields)
                {
                    from = from.Replace("{{" + field.Key + "}}", field.Value);
                    subject = subject.Replace("{{" + field.Key + "}}", field.Value);
                    body = body.Replace("{{" + field.Key + "}}", field.Value);
                }
            }
            return await _emailService.SendEmailAsync(from.ToString(), to, subject.ToString(), body.ToString());
        }
    }
}
