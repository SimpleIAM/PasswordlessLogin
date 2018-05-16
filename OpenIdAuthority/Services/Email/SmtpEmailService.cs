// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using SimpleIAM.OpenIdAuthority.Configuration;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private SmtpConfig _smtpConfig;

        public SmtpEmailService(SmtpConfig smtpConfig)
        {
            _smtpConfig = smtpConfig;
        }

        public async Task<SendMessageResult> SendEmailAsync(string from, string to, string subject, string body)
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
                try
                {
                    await client.ConnectAsync(_smtpConfig.Server, _smtpConfig.Port, _smtpConfig.UseSsl);
                    await client.AuthenticateAsync(_smtpConfig.Username, _smtpConfig.Password);
                    await client.SendAsync(message);
                }
                catch (Exception ex) when (ItIsANetworkError(ex))
                {
                    //todo: log warning
                    return SendMessageResult.Failed("Failed to send email. Please try again.");
                }
                catch (Exception ex) when (ItIsANetworkError(ex))
                {
                    //todo: log error
                    return SendMessageResult.Failed("Failed to send email");
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
            return SendMessageResult.Success();
        }

        private bool ItIsANetworkError(Exception ex)
        {
            return
                ex is System.Net.Sockets.SocketException ||
                ex is System.IO.IOException ||
                ex is MailKit.ProtocolException;
        }
    }
}
