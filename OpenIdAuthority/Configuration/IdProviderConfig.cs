// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public class IdProviderConfig
    {
        public string DisplayName { get; set; } = OpenIdAuthorityConstants.DefaultDisplayName;
        public int DefaultSessionLengthMinutes { get; set; } = OpenIdAuthorityConstants.Security.DefaultDefaultSessionLengthMinutes;
        public int MaxSessionLengthMinutes { get; set; } = OpenIdAuthorityConstants.Security.DefaultMaxSessionLengthMinutes;
        public bool RememberUsernames { get; set; } = true;
        public int MinimumPasswordStrengthInBits { get; set; } = OpenIdAuthorityConstants.Security.DefaultMinimumPasswordStrengthInBits;
        public bool BehindProxy { get; set; } = false;
        public CspConfig Csp { get; set; } = new CspConfig();
        public IDictionary<string, string> CustomProperties { get; set; }
    }
}
