// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Email
{
    public interface IEmailTemplateService
    {
        Task<SendMessageResult> SendEmailAsync(string templateName, string to, Dictionary<string, string> fields);        
    }
}
