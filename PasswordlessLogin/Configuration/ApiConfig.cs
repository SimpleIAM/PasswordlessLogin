// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class ApiConfig
    {
        public string Url { get; set; }
        public string[] Scopes { get; set; }
        public string[] Secrets { get; set; }
        public string[] IncludeClaimTypes { get; set; }
    }
}
