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
    }
}
