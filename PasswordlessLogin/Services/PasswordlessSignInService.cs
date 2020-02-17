// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task SignInAsync(string subjectId, string username, IEnumerable<string> authenticationMethods, AuthenticationProperties properties, params Claim[] claims)
        {           
            var authTime = (DateTime.UtcNow.ToUniversalTime().Ticks - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks) / TimeSpan.TicksPerSecond;
            var restrictedClaimsTypes = new string[] { "sub", "name", "amr", "amc", "auth_time" };
            var userClaims = claims?.Where(c => !restrictedClaimsTypes.Contains(c.Value)).ToList() ?? new List<Claim>();
            userClaims.Add(new Claim("sub", subjectId));
            userClaims.Add(new Claim("name", username));
            userClaims.Add(new Claim("auth_time", authTime.ToString()));
            userClaims.Add(new Claim("amc", authenticationMethods.Count().ToString()));
            foreach(var method in authenticationMethods) 
            {
                userClaims.Add(new Claim("amr", method));
            }
            var identity = new ClaimsIdentity(claims, "pwd", "name", "role");
            var principal = new ClaimsPrincipal(identity);
            if (_httpContext.User.Identity.IsAuthenticated && _httpContext.User.GetSubjectId() != subjectId)
            {
                await _httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            await _httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        }

        public async Task SignOutAsync()
        {
            await _httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // We're signed out now, so the UI for this request should show an anonymous user
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
