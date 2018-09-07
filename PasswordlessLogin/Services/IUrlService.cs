// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services
{
    public interface IUrlService
    {
        string GetDefaultRedirectUrl(bool absolute = false);
        string GetForgotPasswordUrl(bool absolute = false);
        string GetMyAccountUrl(bool absolute = false);
        string GetRegisterUrl(bool absolute = false);
        string GetSetPasswordUrl(bool absolute = false);
        string GetSignInUrl(bool absolute = false);
        string GetSignInLinkUrl(string longCode, bool absolute = false);
        string GetCancelChangeLinkUrl(string longCode, bool absolute = false);
        string GetSignOutUrl(bool absolute = false);
        bool IsAllowedRedirectUrl(string url);
    }
}
