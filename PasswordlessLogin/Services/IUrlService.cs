// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services
{
    public interface IUrlService
    {
        bool IsAllowedRedirectUrl(string url);
        string GetDefaultRedirectUrl();
        string GetRegisterUrl();
        string GetSignInUrl();
        string GetSignInLinkUrl(string longCode);
        string GetMyAccountUrl();
        string GetSetPasswordUrl();
    }
}
