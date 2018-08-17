// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Claims;
using System.Security.Principal;

namespace SimpleIAM.PasswordlessLogin
{
    public static class PrincipalExtensions
    {
        public static string GetClaim(this IPrincipal principal, string claimType)
        {
            return GetClaim(principal?.Identity, claimType);
        }

        public static string GetClaim(this IIdentity identity, string claimType)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            return claimsIdentity?.FindFirst(claimType)?.Value;
        }

        public static string GetSubjectId(this IPrincipal principal)
        {
            return GetClaim(principal, "sub");
        }
        public static string GetDisplayName(this ClaimsPrincipal principal)
        {
            return principal.Identity.Name;
        }

        public static DateTime GetAuthTimeUTC(this ClaimsPrincipal principal)
        {
            var authTimeString = principal.GetClaim("auth_time");
            int.TryParse(authTimeString, out int authTimeInt);
            var authTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            authTime = authTime.AddSeconds(authTimeInt);
            return authTime;
        }
    }
}
