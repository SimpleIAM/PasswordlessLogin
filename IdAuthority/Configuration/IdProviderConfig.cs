using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.IdAuthority.Configuration
{
    public enum SignInMethod
    {
        Email,
        Password
    }
    public class IdProviderConfig
    {
        public string DisplayName { get; set; }
        public SignInMethod DefaultSignInMethod { get; set; } = SignInMethod.Email;
        public int DefaultSessionLengthMinutes { get; set; } = 720; // 12 hours
        public int MaxSessionLengthMinutes { get; set; } = 44640; // 31 days

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
