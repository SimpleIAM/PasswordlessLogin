// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public class PasswordlessSignInService : ISignInService
    {
        private readonly HttpContext _httpContext;

        public PasswordlessSignInService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
        }

        public async Task SignInAsync(string subjectId, string username, AuthenticationProperties authProps)
        {
            var authTime = (DateTime.UtcNow.ToUniversalTime().Ticks - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks) / TimeSpan.TicksPerSecond;
            var claims = new List<Claim> {
                new Claim("sub", subjectId),
                new Claim("name", username),
                new Claim("auth_time", authTime.ToString()),
            };
            var id = new ClaimsIdentity(claims, "pwd", "name", "role");
            var principal = new ClaimsPrincipal(id);
            await _httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
        }

        public async Task SignOutAsync()
        {
            await _httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // We're signed out now, so the UI for this request should show an anonymous user
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
