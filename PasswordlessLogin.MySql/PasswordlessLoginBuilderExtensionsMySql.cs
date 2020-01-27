// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Entities;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginBuilderExtensionsMySql
    {
        public static PasswordlessLoginBuilder AddSqlServer(this PasswordlessLoginBuilder builder, 
            Action<DbContextOptionsBuilder> options)
        {
            builder.Services.AddDbContext<PasswordlessLoginDbContext>(options);

            return builder;
        }
    }
}