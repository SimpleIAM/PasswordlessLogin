// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Http;
using System;

namespace SimpleIAM.PasswordlessLogin
{
    public static class CookieExtensions
    {
        public const string ClientNonceCookieName = PasswordlessLoginConstants.TrustedBrowser.ClientNonceCookieName;
        public const string BrowserIdCookieName = PasswordlessLoginConstants.TrustedBrowser.BrowserIdCookieName;

        public static string GetClientNonce(this HttpRequest request)
        {
            return request.Cookies[ClientNonceCookieName];
        }

        public static string GetBrowserId(this HttpRequest request)
        {
            return request.Cookies[BrowserIdCookieName];
        }

        public static void SetClientNonce(this HttpResponse response, string value, int validityInMinutes)
        {
            response.SetSecureCookie(ClientNonceCookieName, value, TimeSpan.FromMinutes(validityInMinutes));
        }

        public static void SetBrowserId(this HttpResponse response, string value)
        {
            response.SetSecureCookie(BrowserIdCookieName, value, TimeSpan.FromDays(PasswordlessLoginConstants.TrustedBrowser.BrowserIdCookieValidityDays));
        }

        public static void SetSecureCookie(this HttpResponse response, string key, string value, TimeSpan maxAge)
        {
            var options = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.None, // we allow cookies to be sent to us with CORS request
                MaxAge = maxAge, // modern browsers, no need to account for clock skew
                Expires = DateTime.UtcNow.Add(maxAge) // older browsers
            };
            response.Cookies.Append(key, value, options);
        }
    }
}
