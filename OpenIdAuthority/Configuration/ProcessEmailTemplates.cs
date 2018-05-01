using Microsoft.Extensions.Configuration;
using SimpleIAM.OpenIdAuthority.Services.Email;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public static class ProcessEmailTemplates
    {
        public static EmailTemplates GetTemplatesFromMailConfig(IConfigurationSection configuration)
        {
            var defaultFrom = configuration.GetValue<string>("DefaultFrom");
            var templates = new EmailTemplates()
            {
                {
                    "OneTimeCode",
                    new EmailTemplate()
                    {
                        Subject = "One time code",
                        Body = "Here is your one time code: {{one_time_code}}",
                    }
                },
                {
                    "SignInWithEmail",
                    new EmailTemplate()
                    {
                        Subject = "Sign in link",
                        Body = "Use this link to sign in\n{{link}}\n\nor use this one time code: {{one_time_code}}",
                    }
                },
            };
            foreach(var template in templates)
            {
                var custom = new EmailTemplate();
                configuration.Bind($"Templates:{template.Key}", custom);
                template.Value.From = custom.From ?? defaultFrom;
                template.Value.Subject = custom.Subject ?? template.Value.Subject;
                template.Value.Body = custom.Body ?? template.Value.Body;
            }
            return templates;
        }
    }
}
