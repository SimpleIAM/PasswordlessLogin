// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Entities;
using SimpleIAM.PasswordlessLogin.SqlServer;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginBuilderExtensionsSqlServer
    {
        public static PasswordlessLoginBuilder AddSqlServer(this PasswordlessLoginBuilder builder, 
            Action<DbContextOptionsBuilder> options,
            SqlServerPasswordlessDatabaseConfig config = null
            )
        {
            builder.Services.AddSingleton(config ?? new SqlServerPasswordlessDatabaseConfig());

            // First register the inherited db context. However, this registers 
            // DbContextOptions<SqlServerPasswordlessLoginDbContext> which can't be passed to be base class,
            // so we use the "hack" below to register DbContextOptions<PasswordlessLoginDbContext>.
            builder.Services.AddDbContext<PasswordlessLoginDbContext, SqlServerPasswordlessLoginDbContext>(options);

            // Register the base db context in order to register DbContextOptions<PasswordlessLoginDbContext>.
            // PasswordlessLoginDbContext won't actually be used since we already registed a different implementation above.
            builder.Services.AddDbContext<PasswordlessLoginDbContext>(options);

            return builder;
        }
    }
}