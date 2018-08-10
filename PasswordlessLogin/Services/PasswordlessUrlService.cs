// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public class PasswordlessUrlService : IUrlService
    {
        private readonly IUrlHelper _urlHelper;
        private readonly HttpContext _httpContext;

        public PasswordlessUrlService(
            IUrlHelper urlHelper,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _urlHelper = urlHelper;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public string GetDefaultRedirectUrl()
        {
            return _urlHelper.Action("Apps", "Home");
        }

        public string GetMyAccountUrl()
        {
            return _urlHelper.Action("Index", "Account", new { }, _httpContext.Request.Scheme);
        }

        public string GetRegisterUrl()
        {
            return _urlHelper.Action("Register", "Authenticate", new { }, _httpContext.Request.Scheme);
        }

        public string GetSetPasswordUrl()
        {
            return _urlHelper.Action("SetPassword", "Account", new { }, _httpContext.Request.Scheme);
        }

        public string GetSignInLinkUrl(string longCode)
        {
            return _urlHelper.Action("SignInLink", "Authenticate", new { longCode }, _httpContext.Request.Scheme);
        }

        public string GetSignInUrl()
        {
            return _urlHelper.Action("SignIn", "Authenticate", new { }, _httpContext.Request.Scheme);
        }

        public bool IsAllowedRedirectUrl(string url)
        {
            return _urlHelper.IsLocalUrl(url);
        }
    }
}
