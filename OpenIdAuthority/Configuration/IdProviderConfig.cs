// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public enum SignInMethod
    {
        Email,
        Password
    }
    public class IdProviderConfig
    {
        public string DisplayName { get; set; } = "OpenID Authority";
        public SignInMethod DefaultSignInMethod { get; set; } = SignInMethod.Email;
        public int DefaultSessionLengthMinutes { get; set; } = 720; // 12 hours
        public int MaxSessionLengthMinutes { get; set; } = 44640; // 31 days
        public bool RememberUsernames { get; set; } = true;
        public int MinimumPasswordStrengthInBits { get; set; } = 40;

        public string SignInAction {
            get {
                switch(DefaultSignInMethod)
                {
                    case SignInMethod.Password:
                        return "SignInPass";
                    default:
                        return "SignIn";
                }
            }
        }
    }
}
