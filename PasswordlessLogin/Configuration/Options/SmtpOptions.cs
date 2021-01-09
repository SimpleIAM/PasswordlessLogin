// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class SmtpOptions
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseSsl { get; set; } = true;
        /// <summary>
        /// Use the configured Username and Password to authenticate with the SMTP server, otherwise no auth is attempted (typically anonymous).
        /// </summary>
        public bool UseAuthentication { get; set; } = true;
    }
}
