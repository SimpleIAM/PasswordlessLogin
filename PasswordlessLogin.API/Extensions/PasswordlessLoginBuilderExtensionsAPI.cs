// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin;
using SimpleIAM.PasswordlessLogin.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginBuilderExtensionsAPI
    {
        public static PasswordlessLoginBuilder AddPasswordlessLoginAPI(this PasswordlessLoginBuilder builder, string[] apiAllowedOrigins = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
     
            builder.Services.AddPasswordlessCorsPolicy(apiAllowedOrigins);

            return builder;
        }

        private static IServiceCollection AddPasswordlessCorsPolicy(this IServiceCollection services, string[] allowedOrigins = null)
        {
            if (allowedOrigins?.Length > 0)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(PasswordlessLoginConstants.Security.CorsPolicyName, builder => builder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(PasswordlessLoginConstants.Security.CorsPolicyName, builder => builder
                        .DisallowCredentials());
                });
            }

            return services;
        }
    }
}
