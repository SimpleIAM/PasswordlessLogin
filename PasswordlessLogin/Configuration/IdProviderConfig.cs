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
        public int CancelEmailChangeTimeWindowHours { get; set; } = PasswordlessLoginConstants.Security.DefaultCancelEmailChangeTimeWindowHours;
        public bool RememberUsernames { get; set; } = true;
        public int MinimumPasswordLength { get; set; } = PasswordlessLoginConstants.Security.DefaultMinimumPasswordLength;
        public int MinimumPasswordStrengthInBits { get; set; } = PasswordlessLoginConstants.Security.DefaultMinimumPasswordStrengthInBits;
        public int MaxPasswordFailedAttempts { get; set; } = PasswordlessLoginConstants.Security.DefaultMaxPasswordFailedAttempts;
        public int TempLockPasswordMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultTempLockPasswordMinutes;
        public bool ResetFailedAttemptCountOnTempLock { get; set; } = false;
        public int OneTimeCodeValidityMinutes { get; set; } = PasswordlessLoginConstants.OneTimeCode.DefaultValidityMinutes;
        public int ConfirmAccountLinkValidityMinutes { get; set; } = PasswordlessLoginConstants.OneTimeCode.ConfirmAccountDefaultValidityMinutes;
        public bool ResendWelcomeEmailOnReRegister { get; set; } = true;
        public UrlConfig Urls { get; set; }
        public IDictionary<string, string> CustomProperties { get; set; }
    }
}
