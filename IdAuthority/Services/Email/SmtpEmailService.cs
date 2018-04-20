// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using SimpleIAM.IdAuthority.Configuration;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private SmtpConfig _smtpConfig;

        public SmtpEmailService(SmtpConfig smtpConfig)
        {
            _smtpConfig = smtpConfig;
        }

        public async Task SendEmailAsync(string from, string to, string subject, string body)
        {
            var message = new MimeMessage
            {
                Sender = new MailboxAddress(from),
                Subject = subject
            };
            message.To.Add(new MailboxAddress(to));
            if (body?.Contains("<") == true)
            {
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = body
                };
            }
            else
            {
                message.Body = new TextPart(TextFormat.Plain)
                {
                    Text = body
                };
            }

            using(var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpConfig.Server, _smtpConfig.Port, _smtpConfig.UseSsl);
                await client.AuthenticateAsync(_smtpConfig.Username, _smtpConfig.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
