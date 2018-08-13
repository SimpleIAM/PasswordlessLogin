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

        public string GetDefaultRedirectUrl()
        {
            return AbsoluteUrl(_urls.DefaultRedirect);
        }

        public string GetForgotPasswordUrl()
        {
            return AbsoluteUrl(_urls.ForgotPassword);
        }

        public string GetMyAccountUrl()
        {
            return AbsoluteUrl(_urls.MyAccount);
        }

        public string GetRegisterUrl()
        {
            return AbsoluteUrl(_urls.Register);
        }

        public string GetSetPasswordUrl()
        {
            return AbsoluteUrl(_urls.SetPassword);
        }

        public string GetSignInLinkUrl(string longCode)
        {
            return AbsoluteUrl(_urls.SignInLink.Replace("{long_code}", longCode));
        }

        public string GetSignInUrl()
        {
            return AbsoluteUrl(_urls.SignIn);
        }

        public string GetSignOutUrl()
        {
            return AbsoluteUrl(_urls.SignOut);
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
