// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class IdScopeConfig
    {
        public string Name { get; set; }
        public string[] IncludeClaimTypes { get; set; }
        public bool Required { get; set; }
    }
}
