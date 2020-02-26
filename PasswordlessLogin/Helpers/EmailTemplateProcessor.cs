// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.FileProviders;
using SimpleIAM.PasswordlessLogin.Services.Email;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleIAM.PasswordlessLogin.Helpers
{
    public static class EmailTemplateProcessor
    {
        public static EmailTemplates GetTemplates(IFileProvider fileProvider, CultureInfo[] supportedCultures)
        {
            var templates = new EmailTemplates();
            var cultureSuffixes = supportedCultures == null
                ? new List<string>()
                : supportedCultures.Select(ci => $".{ci.Name}").ToList();
            cultureSuffixes.Add(""); // For the default/fallback template

            foreach (var templateKey in PasswordlessLoginConstants.EmailTemplates.All)
            {
                foreach (var cultureSuffix in cultureSuffixes)
                {
                    var fileInfo = fileProvider.GetFileInfo($"{templateKey}{cultureSuffix}.html");
                    if (fileInfo.Exists)
                    {
                        var template = new EmailTemplate();
                        using (var reader = new StreamReader(fileInfo.CreateReadStream()))
                        {
                            string data = reader.ReadToEnd();
                            template.Subject = (new Regex(@"\<title\>.*\<\/title\>", RegexOptions.Singleline)).Match(data).Value.Replace("<title>", "").Replace("</title>", "").Trim(' ', '\t', '\n', '\r');
                            template.Body = (new Regex(@"\<body\>.*\<\/body\>", RegexOptions.Singleline)).Match(data).Value.Replace("<body>", "").Replace("</body>", "").Trim(' ', '\t', '\n', '\r');
                        }
                        templates.Add($"{templateKey}{cultureSuffix}", template);
                    }
                }
            }
            return templates;
        }
    }
}
