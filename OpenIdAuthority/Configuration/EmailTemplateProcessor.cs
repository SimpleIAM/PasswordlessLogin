// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SimpleIAM.OpenIdAuthority.Services.Email;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public static class EmailTemplateProcessor
    {
        public static EmailTemplates GetTemplatesFromMailConfig(string defaultFrom, IFileProvider fileProvider)
        {
            var templates = new EmailTemplates()
            {
                { OpenIdAuthorityConstants.EmailTemplates.OneTimeCode, new EmailTemplate() },
                { OpenIdAuthorityConstants.EmailTemplates.SignInWithEmail, new EmailTemplate() },
                { OpenIdAuthorityConstants.EmailTemplates.Welcome, new EmailTemplate() },
                { OpenIdAuthorityConstants.EmailTemplates.PasswordReset, new EmailTemplate() },
                { OpenIdAuthorityConstants.EmailTemplates.AccountNotFound, new EmailTemplate() },
            };
            foreach(var template in templates)
            {
                template.Value.From = defaultFrom;

                var fileInfo = fileProvider.GetFileInfo($"{template.Key}.html");
                if(fileInfo.Exists)
                {
                    using (var reader = new StreamReader(fileInfo.CreateReadStream()))
                    {
                        string data = reader.ReadToEnd();
                        template.Value.Subject = (new Regex(@"\<title\>.*\<\/title\>", RegexOptions.Singleline)).Match(data).Value.Replace("<title>", "").Replace("</title>", "").Trim(' ', '\t', '\n', '\r');
                        template.Value.Body = (new Regex(@"\<body\>.*\<\/body\>", RegexOptions.Singleline)).Match(data).Value.Replace("<body>", "").Replace("</body>", "").Trim(' ', '\t', '\n', '\r');
                    }
                }
            }
            return templates;
        }
    }
}
