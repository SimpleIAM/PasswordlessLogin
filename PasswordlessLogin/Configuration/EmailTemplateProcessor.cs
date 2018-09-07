// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.FileProviders;
using SimpleIAM.PasswordlessLogin.Services.Email;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public static class EmailTemplateProcessor
    {
        public static EmailTemplates GetTemplatesFromMailConfig(string defaultFrom, IFileProvider fileProvider)
        {
            var templates = new EmailTemplates()
            {
                { PasswordlessLoginConstants.EmailTemplates.OneTimeCode, new EmailTemplate() },
                { PasswordlessLoginConstants.EmailTemplates.SignInWithEmail, new EmailTemplate() },
                { PasswordlessLoginConstants.EmailTemplates.Welcome, new EmailTemplate() },
                { PasswordlessLoginConstants.EmailTemplates.PasswordReset, new EmailTemplate() },
                { PasswordlessLoginConstants.EmailTemplates.PasswordChangedNotice, new EmailTemplate() },
                { PasswordlessLoginConstants.EmailTemplates.PasswordRemovedNotice, new EmailTemplate() },
                { PasswordlessLoginConstants.EmailTemplates.EmailChangedNotice, new EmailTemplate() },
                { PasswordlessLoginConstants.EmailTemplates.AccountNotFound, new EmailTemplate() },
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
