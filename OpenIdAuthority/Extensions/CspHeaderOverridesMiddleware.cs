// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Extensions
{
    public class CspHeaderOverridesMiddleware
    {
        private readonly RequestDelegate _next;

        public CspHeaderOverridesMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == OpenIdAuthorityConstants.Configuration.CheckSessionIFrame)
            {
                // allow check_session_iframe to be loaded in an iframe
                context.Response.Headers.Remove("Content-Security-Policy");
                context.Response.Headers.Remove("X-Frame-Options");
            }
            await _next(context);
        }
    }
}
