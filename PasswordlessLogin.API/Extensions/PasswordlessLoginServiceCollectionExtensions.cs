// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginServiceCollectionExtensions
    {
        public static IServiceCollection AddPasswordlessLoginAPI(this IServiceCollection services, string[] apiAllowedOrigins = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
     
            services.AddPasswordlessCorsPolicy(apiAllowedOrigins);

            return services;
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
