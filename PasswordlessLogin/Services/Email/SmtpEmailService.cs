// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using SimpleIAM.PasswordlessLogin.Configuration;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger _logger;
        private readonly SmtpConfig _smtpConfig;

        public SmtpEmailService(ILogger<SmtpEmailService> logger, SmtpConfig smtpConfig)
        {
            _logger = logger;
            _smtpConfig = smtpConfig;
        }

        public async Task<SendMessageResult> SendEmailAsync(string from, string to, string subject, string body)
        {
            _logger.LogDebug("Sending email to {0} with subject {1}", to, subject);
            var message = new MimeMessage()
            {                
                Subject = subject
            };
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(to));
            if (body?.Contains("</") == true || body?.Contains("/>") == true)
            {
                _logger.LogTrace("Body of message is html");
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = body
                };
                //todo: add a plain-text version also for better email delivery
            }
            else
            {
                _logger.LogTrace("Body of message is plain text");
                message.Body = new TextPart(TextFormat.Plain)
                {
                    Text = body
                };
            }

            using(var client = new SmtpClient())
            {
                try
                {
                    _logger.LogDebug("Connecting to {0}:{1} ({2})", _smtpConfig.Server, _smtpConfig.Port, _smtpConfig.UseSsl ? "ssl" : "not ssl");
                    await client.ConnectAsync(_smtpConfig.Server, _smtpConfig.Port, _smtpConfig.UseSsl);
                    _logger.LogDebug("Authenticating with SMTP server");
                    await client.AuthenticateAsync(_smtpConfig.Username, _smtpConfig.Password);
                    _logger.LogDebug("Sending message");
                    await client.SendAsync(message);
                }
                catch (Exception ex) when (ItIsANetworkError(ex))
                {
                    _logger.LogError("Network error. Failed to send message. Exception: {0}", ex.ToString());
                    return SendMessageResult.Failed("Failed to send email. Please try again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to send message. Exception: {0}", ex.ToString());
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
