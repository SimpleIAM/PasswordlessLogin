﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class HostingConfig
    {
        public bool BehindProxy { get; set; } = false;

        public bool SkipContentSecuritySetup { get; set; } = false;

        public CspConfig Csp { get; set; } = new CspConfig();
    }
}
