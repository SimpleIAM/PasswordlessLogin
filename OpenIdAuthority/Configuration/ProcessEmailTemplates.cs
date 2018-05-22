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
                        Subject = "One time code and sign in link",
                        Body = "Use this one time code to sign in: <strong>{{one_time_code}}</strong><br /><br />Or use this link:<br /><a href=\"{{sign_in_link}}\">{{sign_in_link}}</a>",
                    }
                },
                {
                    "Welcome",
                    new EmailTemplate()
                    {
                        Subject = "Please confirm your account",
                        Body = "Thanks for registering!<br/><br />Please use this link to confirm your account and sign in:<br /><a href=\"{{sign_in_link}}\">{{sign_in_link}}</a><br /><br />Or you can use the one time code <strong>{{one_time_code}}</strong> to <a href=\"{{sign_in_url}}\">sign in</a>",
                    }
                },
                {
                    "PasswordReset",
                    new EmailTemplate()
                    {
                        Subject = "Password Reset",
                        Body = "Use this link to reset your password: <br /><a href=\"{{password_reset_link}}\">{{password_reset_link}}</a>",
                    }
                },
                {
                    "AccountNotFound",
                    new EmailTemplate()
                    {
                        Subject = "Account Not Found",
                        Body = "We didn't find an account associated with this address. You can register here: <br /><a href=\"{{register_link}}\">{{register_link}}</a>",
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
