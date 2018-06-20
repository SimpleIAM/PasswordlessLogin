// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    internal class ReconfigureCookieOptions : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        public ReconfigureCookieOptions()
        {
        }

        public void Configure(CookieAuthenticationOptions options)
        {
        }

        public void Configure(string name, CookieAuthenticationOptions options)
        {
            options.Cookie.IsEssential = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Events.OnRedirectToAccessDenied = context =>
            {
                // Don't redirect to another page
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return Task.FromResult(0);
            };
        }
    }
}
