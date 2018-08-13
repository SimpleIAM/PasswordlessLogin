// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services
{
    public interface IUrlService
    {
        string GetDefaultRedirectUrl();
        string GetForgotPasswordUrl();
        string GetMyAccountUrl();
        string GetRegisterUrl();
        string GetSetPasswordUrl();
        string GetSignInUrl();
        string GetSignInLinkUrl(string longCode);
        string GetSignOutUrl();
        bool IsAllowedRedirectUrl(string url);
    }
}
