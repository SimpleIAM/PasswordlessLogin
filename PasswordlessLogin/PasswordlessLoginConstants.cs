// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

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
            public const string OneTimeCode = "OneTimeCode";
            public const string SignInWithEmail = "SignInWithEmail";
            public const string Welcome = "Welcome";
            public const string PasswordReset = "PasswordReset";
            public const string AccountNotFound = "AccountNotFound";
        }

        public static class OneTimeCode
        {
            public const int DefaultValidityMinutes = 15;
            public const int ConfirmAccountDefaultValidityMinutes = 1440;
            public const int IssueNewCodeIfValidityLessThanXMinutes = 3;
            public const int MaxFailedAttemptCount = 3;
            public const int MaxResendCount = 4;
            public const int ShortCodeLength = 6;
            public const int LongCodeMaxLength = 36;
        }

        public static class RecognizedDevices
        {
            public const string ClientNonceCookieName = "ClientNonce";
            public const string DeviceIdCookieName = "DeviceId";
            public const int DeviceIdCookieValidityDays = 3650;
        }

        public static class Security
        {
            public const string CorsPolicyName = "PasswordlessCorsPolicy";
            public const int DefaultPbkdf2Iterations = 50000;
            public const int DefaultMinimumPasswordLength = 8;
            public const int DefaultMinimumPasswordStrengthInBits = 30;
            public const int DefaultDefaultSessionLengthMinutes = 720; // 12 hours
            public const int DefaultMaxSessionLengthMinutes = 44640; // 31 days
        }
    }
}
