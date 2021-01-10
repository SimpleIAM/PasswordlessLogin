using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using PasswordlessLogin.Tests.TestHelper;
using SimpleIAM.PasswordlessLogin.Services;
using Xunit;

namespace PasswordlessLogin.Tests.Services.SigninService
{
    public class SigninServiceTests : MockedHttpContextTest
    {
        [Theory]
        [InlineData("name")]
        [InlineData("sub")]
        [InlineData("auth_time")]
        [InlineData("amc")]
        public async Task SignInAsyncShouldHaveClaimType(string claimType)
        {
            var subjectId = Guid.NewGuid().ToString();
            var service = new PasswordlessSignInService(_httpContextAccessor);
            const string username = "user@domain.com";
            var authenticationMethods = new List<string> {"pwd"};
            var authProps = new AuthenticationProperties();

            await service.SignInAsync(subjectId, username, authenticationMethods, authProps);

            // Verify we called SignIn and have the expected claim type
            await _authenticationService.Received().SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string>(),
                Arg.Is<ClaimsPrincipal>(cp => cp.HasClaim(claim => claim.Type.Equals(claimType))),
                Arg.Any<AuthenticationProperties>());
        }
    }
}