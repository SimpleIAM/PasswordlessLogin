// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public class IdProviderConfig
    {
        public string DisplayName { get; set; } = "OpenID Authority";
        public int DefaultSessionLengthMinutes { get; set; } = 720; // 12 hours
        public int MaxSessionLengthMinutes { get; set; } = 44640; // 31 days
        public bool RememberUsernames { get; set; } = true;
        public int MinimumPasswordStrengthInBits { get; set; } = 40;
        public bool BehindProxy { get; set; } = false;
        public CspConfig Csp { get; set; } = new CspConfig();
        public IDictionary<string, string> CustomProperties { get; set; }
    }
}
