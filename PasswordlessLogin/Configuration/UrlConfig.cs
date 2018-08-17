// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class UrlConfig
    {
        public string DefaultRedirect { get; set; } = "/";
        public string MyAccount { get; set; } = "/account";
        public string ForgotPassword { get; set; } = "/forgotpassword";
        public string Register { get; set; } = "/register";
        public string SetPassword { get; set; } = "/account/setpassword";
        public string SignIn { get; set; } = "/signin";
        public string SignInLink { get; set; } = "/signin/{long_code}";
        public string SignOut { get; set; } = "/signout";
        public string ApiBase { get; set; } = "/passwordless-api";
        public string CustomApiBase { get; set; } = "/api";
        public string Error { get; set; } = "/error";
    }
}
