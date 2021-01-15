// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Configuration;
using SimpleIAM.PasswordlessLogin;
using SimpleIAM.PasswordlessLogin.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginServiceCollectionExtensions
    {
        public static PasswordlessLoginBuilder AddPasswordlessLogin(this IServiceCollection services, Action<PasswordlessLoginOptions> optionsAction = null)
        {
            var passwordlessLoginOptions = new PasswordlessLoginOptions();
            optionsAction?.Invoke(passwordlessLoginOptions);

            var builder = new PasswordlessLoginBuilder(services, passwordlessLoginOptions);
            return builder.AddPasswordlessLogin();
        }

        public static PasswordlessLoginBuilder AddPasswordlessLogin(this IServiceCollection services, IConfiguration configuration)
        {
            var passwordlessLoginOptions = new PasswordlessLoginOptions();
            configuration.GetSection(PasswordlessLoginConstants.ConfigurationSections.IdProvider).Bind(passwordlessLoginOptions);
            var builder = new PasswordlessLoginBuilder(services, passwordlessLoginOptions);
            return builder.AddPasswordlessLogin(configuration);
        }
    }
}
