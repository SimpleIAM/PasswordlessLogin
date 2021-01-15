// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using StandardResponse;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Email
{
    public interface IEmailService
    {
        Task<Status> SendEmailAsync(EmailAddress from, string to, string subject, string body);
    }
}
