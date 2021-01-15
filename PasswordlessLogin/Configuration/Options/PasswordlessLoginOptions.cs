﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class PasswordlessLoginOptions
    {
        public PasswordlessLoginOptions()
        {
            Urls = new UrlOptions();
        }

        public string[] IdpUserClaims { get; set; } = new string[] { };
        public string IdpUserNameClaim { get; set; } = "email";
        public int DefaultSessionLengthMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultDefaultSessionLengthMinutes;
        public int MaxSessionLengthMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultMaxSessionLengthMinutes;
        public int ChangeSecuritySettingsTimeWindowMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultChangeSecuritySettingsTimeWindowMinutes;
        public bool SendCancelEmailChangeMessage { get; set; } = true;
        public int CancelEmailChangeTimeWindowHours { get; set; } = PasswordlessLoginConstants.Security.DefaultCancelEmailChangeTimeWindowHours;
        public bool RememberUsernames { get; set; } = true;
        public int MinimumPasswordLength { get; set; } = PasswordlessLoginConstants.Security.DefaultMinimumPasswordLength;
        public int MinimumPasswordStrengthInBits { get; set; } = PasswordlessLoginConstants.Security.DefaultMinimumPasswordStrengthInBits;
        public int TempLockPasswordFailedAttemptCount { get; set; } = PasswordlessLoginConstants.Security.DefaultTempLockPasswordFailedAttemptCount;
        public int TempLockPasswordMinutes { get; set; } = PasswordlessLoginConstants.Security.DefaultTempLockPasswordMinutes;
        public int MaxUntrustedPasswordFailedAttempts { get; set; } = PasswordlessLoginConstants.Security.DefaultMaxUntrustedPasswordFailedAttempts;
        public int OneTimeCodeValidityMinutes { get; set; } = PasswordlessLoginConstants.OneTimeCode.DefaultValidityMinutes;
        public int ConfirmAccountLinkValidityMinutes { get; set; } = PasswordlessLoginConstants.OneTimeCode.ConfirmAccountDefaultValidityMinutes;
        public bool AutoTrustBrowsers { get; set; } = true;
        public bool NonceRequiredOnUntrustedBrowser { get; set; } = true;
        public UrlOptions Urls { get; set; }
        public IDictionary<string, string> CustomProperties { get; set; }
        public SameSiteMode CookieSameSiteMode { get; set; } = SameSiteMode.Unspecified;

        public MailOptions Mail { get; set; } = new MailOptions();
    }
}
