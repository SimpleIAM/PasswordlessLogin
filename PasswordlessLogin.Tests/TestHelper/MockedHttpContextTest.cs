using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace PasswordlessLogin.Tests.TestHelper
{
    public class MockedHttpContextTest
    {
        private protected IHttpContextAccessor _httpContextAccessor;
        private protected IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
        private protected IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();

        public MockedHttpContextTest()
        {
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            SetupMockServiceProvider();
            _httpContextAccessor.HttpContext.RequestServices = _serviceProvider;
        }

        private void SetupMockServiceProvider()
        {
            _authenticationService
                .SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(),
                    Arg.Any<AuthenticationProperties>()).Returns(Task.FromResult((object) null));

            var authSchemaProvider = Substitute.For<IAuthenticationSchemeProvider>();
            var systemClock = Substitute.For<ISystemClock>();

            authSchemaProvider.GetDefaultAuthenticateSchemeAsync().Returns(Task.FromResult
            (new AuthenticationScheme("idp", "idp",
                typeof(IAuthenticationHandler))));

            _serviceProvider.GetService(typeof(IAuthenticationService)).Returns(_authenticationService);
            _serviceProvider.GetService(typeof(ISystemClock)).Returns(systemClock);
            _serviceProvider.GetService(typeof(IAuthenticationSchemeProvider)).Returns(authSchemaProvider);
        }
    }
}