// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Email
{
    public class ConsoleEmailService : IEmailService
    {
        public async Task SendEmailAsync(string from, string to, string subject, string body)
        {
            Console.WriteLine("---Email---");
            Console.Write("From: ");
            Console.WriteLine(from);
            Console.Write("To: ");
            Console.WriteLine(to);
            Console.Write("Subject: ");
            Console.WriteLine(subject);
            Console.WriteLine("---");
            Console.WriteLine(body);
            Console.WriteLine("---End Email---");
        }
    }
}
