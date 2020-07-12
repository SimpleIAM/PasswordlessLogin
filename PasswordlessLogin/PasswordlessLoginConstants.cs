// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin
{
    public static class PasswordlessLoginConstants
    {
        public const string DefaultDisplayName = "Passwordless Login";

        public const string BasicEmailRegexPattern = @".+\@.+\..+";
        public const string EmailTemplateFolder = "EmailTemplates";

        public static class ConfigurationSections
        {
            public const string IdProvider = "IdProvider";
            public const string PasswordlessDatabase = "PasswordlessDatabase";
            public const string MailFrom = "Mail:From";
            public const string Smtp = "Mail:Smtp";
            public const string ConnectionStringName = "DefaultConnection";
        }

        public static class EmailTemplates
        {
            public const string AccountAlreadyExists = "AccountAlreadyExists";
            public const string AccountNotFound = "AccountNotFound";
            public const string EmailChangedNotice = "EmailChangedNotice";
            public const string OneTimeCode = "OneTimeCode";
            public const string PasswordChangedNotice = "PasswordChangedNotice";
            public const string PasswordRemovedNotice = "PasswordRemovedNotice";
            public const string PasswordReset = "PasswordReset";
            public const string SignInWithEmail = "SignInWithEmail";
            public const string Welcome = "Welcome";
            
            public static string[] All = {
                AccountAlreadyExists, 
                AccountNotFound, 
                EmailChangedNotice, 
                OneTimeCode,
                PasswordChangedNotice,
                PasswordRemovedNotice,
                PasswordReset,
                SignInWithEmail,
                Welcome
            };
        }

        public static class OneTimeCode
        {
            public const int DefaultValidityMinutes = 15; // 15 minutes is short enough to prevent abuse and long enough to protect against brute force of a 6-digit code
            public const int ConfirmAccountDefaultValidityMinutes = 1440;
            public const int IssueNewCodeIfValidityLessThanXMinutes = 3;
            public const int MaxFailedAttemptCount = 3;
            public const int MaxResendCount = 4;
            public const int ShortCodeLength = 6;
            public const int LongCodeMaxLength = 36;
        }

        public static class TrustedBrowser
        {
            public const string ClientNonceCookieName = "ClientNonce";
            public const string BrowserIdCookieName = "BrowserId";
            public const int BrowserIdCookieValidityDays = 7300;
        }

        public static class Security
        {
            public const string CorsPolicyName = "PasswordlessCorsPolicy";
            public const int DefaultPbkdf2Iterations = 50000;
            public const int DefaultMinimumPasswordLength = 8;
            public const int DefaultMinimumPasswordStrengthInBits = 30;
            public const int DefaultTempLockPasswordFailedAttemptCount = 5;
            public const int DefaultTempLockPasswordMinutes = 5;
            public const int DefaultMaxUntrustedPasswordFailedAttempts = 15;
            public const int DefaultDefaultSessionLengthMinutes = 720; // 12 hours
            public const int DefaultMaxSessionLengthMinutes = 44640; // 31 days
            public const int DefaultChangeSecuritySettingsTimeWindowMinutes = 5;
            public const int DefaultCancelEmailChangeTimeWindowHours = 72; // 3 days
            public const string PreviousEmailClaimType = "__previous_email";
            public const string EmailNotConfirmedClaimType = "__email_not_confirmed";
            public static readonly string[] ForbiddenClaims = { "iss", "sub", "aud", "exp", "iat", "auth_time", "nonce", "acr", "amr", "azp", "email" };
            public static readonly string[] ProtectedClaims = { PreviousEmailClaimType, EmailNotConfirmedClaimType };
        }
    }
}
