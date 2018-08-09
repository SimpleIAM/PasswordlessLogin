using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin
{
    public static class HttpContextExtensions
    {
        //public static async Task SignInAsync(this HttpContext context, string subject, string name, AuthenticationProperties properties)
        //{
        //    var authTime = (DateTime.UtcNow.ToUniversalTime().Ticks - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks) / TimeSpan.TicksPerSecond;
        //    var claims = new List<Claim> {
        //        new Claim("sub", subject),
        //        new Claim("name", name),
        //        new Claim("auth_time", authTime.ToString()),
        //    };
        //    var id = new ClaimsIdentity(claims, "pwd", "rname", "role");
        //    var principal = new ClaimsPrincipal(id);
        //    await context.SignInAsync("Cookies", principal, properties);
        //}
    }
}
