// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class IdProviderConfig
    {
        public IdProviderConfig()
        {
            Urls = new UrlConfig();
        }

        public string DisplayName { get; set; } = PasswordlessLoginConstants.DefaultDisplayName;
        public int DefaultSessionLengthMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultDefaultSessionLengthMinutes;
        public int MaxSessionLengthMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultMaxSessionLengthMinutes;
        public int ChangeSecuritySettingsTimeWindowMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultChangeSecuritySettingsTimeWindowMinutes;
        public bool RememberUsernames { get; set; } = true;
        public int MinimumPasswordLength { get; set; } = PasswordlessLoginConstants.Security.DefaultMinimumPasswordLength;
        public int MinimumPasswordStrengthInBits { get; set; } = PasswordlessLoginConstants.Security.DefaultMinimumPasswordStrengthInBits;
        public UrlConfig Urls { get; set; }
        public IDictionary<string, string> CustomProperties { get; set; }
    }
}
