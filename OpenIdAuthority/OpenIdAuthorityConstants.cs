// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority
{
    public static class OpenIdAuthorityConstants
    {
        public const string DefaultDisplayName = "OpenID Authority";

        public const string BasicEmailRegexPattern = @".+\@.+\..+";
        public const string EmailTemplateFolder = "EmailTemplates";

        public static class Configuration
        {
            public const string LoginUrl = "/signin";
            public const string LogoutUrl = "/signout";
            public const string LogoutIdParameter = "id";
            public const string ErrorUrl = "/error";
            public const string CheckSessionIFrame = "/connect/checksession";
        }

        public static class ConfigurationSections
        {
            public const string Apis = "Apis";
            public const string Apps = "Apps";
            public const string Hosting = "Hosting";
            public const string IdProvider = "IdProvider";
            public const string IdScopes = "IdScopes";
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
            public const string CorsPolicyName = "CorsPolicy";
            public const int DefaultPbkdf2Iterations = 50000;
            public const int DefaultMinimumPasswordStrengthInBits = 30;
            public const int DefaultDefaultSessionLengthMinutes = 720; // 12 hours
            public const int DefaultMaxSessionLengthMinutes = 44640; // 31 days
        }

        public static class StandardClaims
        {
            public const string Name = "name";
            public const string Email = "email";
            public const string EmailVerified = "email_verified";
        }
    }
}
