// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Configuration;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public class PasswordlessUrlService : IUrlService
    {
        private readonly IUrlHelper _urlHelper;
        private readonly HttpContext _httpContext;
        private readonly UrlConfig _urls;

        public PasswordlessUrlService(
            IUrlHelper urlHelper,
            IHttpContextAccessor httpContextAccessor,
            IdProviderConfig idProviderConfig
            )
        {
            _urlHelper = urlHelper;
            _httpContext = httpContextAccessor.HttpContext;
            _urls = idProviderConfig.Urls;
        }

        public string GetCancelChangeLinkUrl(string longCode, bool absolute = false)
        {
            var url = _urls.CancelChangeLink.Replace("{long_code}", longCode);
            if (absolute)
            {
                return AbsoluteUrl(url);
            }
            return url;
        }

        public string GetDefaultRedirectUrl(bool absolute = false)
        {
            if (absolute)
            {
                return AbsoluteUrl(_urls.DefaultRedirect);
            }
            return _urls.DefaultRedirect;
        }

        public string GetForgotPasswordUrl(bool absolute = false)
        {
            if (absolute)
            {
                return AbsoluteUrl(_urls.ForgotPassword);
            }
            return _urls.ForgotPassword;
        }

        public string GetMyAccountUrl(bool absolute = false)
        {
            if (absolute)
            {
                return AbsoluteUrl(_urls.MyAccount);
            }
            return _urls.MyAccount;
        }

        public string GetRegisterUrl(bool absolute = false)
        {
            if (absolute)
            {
                return AbsoluteUrl(_urls.Register);
            }
            return _urls.Register;
        }

        public string GetSetPasswordUrl(bool absolute = false)
        {
            if (absolute)
            {
                return AbsoluteUrl(_urls.SetPassword);
            }
            return _urls.SetPassword;
        }

        public string GetSignInLinkUrl(string longCode, bool absolute = false)
        {
            var url = _urls.SignInLink.Replace("{long_code}", longCode);
            if (absolute)
            {
                return AbsoluteUrl(url);
            }
            return url;
        }

        public string GetSignInUrl(bool absolute = false)
        {
            if (absolute)
            {
                return AbsoluteUrl(_urls.SignIn);
            }
            return _urls.SignIn;
        }

        public string GetSignOutUrl(bool absolute = false)
        {
            if (absolute)
            {
                return AbsoluteUrl(_urls.SignOut);
            }
            return _urls.SignOut;
        }

        public bool IsAllowedRedirectUrl(string url)
        {
            return _urlHelper.IsLocalUrl(url);
        }

        private string AbsoluteUrl(string url)
        {
            if (url == null)
            {
                return null;
            }
            if (url.StartsWith("https://") || url.StartsWith("http://"))
            {
                return url;
            }
            var separator = "";
            if (!url.StartsWith("/"))
            {
                separator = "/";
            }
            return $"{_httpContext.Request.Scheme}://{_httpContext.Request.Host.ToString()}{separator}{url}";
        }
    }
}
