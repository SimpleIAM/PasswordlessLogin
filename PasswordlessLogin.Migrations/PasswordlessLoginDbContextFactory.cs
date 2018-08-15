// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Entities;

namespace SimpleIAM.PasswordlessLogin.Migrations
{
    public class PasswordlessLoginDbContextFactory : IDesignTimeDbContextFactory<PasswordlessLoginDbContext>
    {
        public PasswordlessLoginDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PasswordlessLoginDbContext>();

            var connection = "(none)";
            var schema = "auth";

            optionsBuilder.UseSqlServer(connection, b =>
            {
                b.MigrationsAssembly("SimpleIAM.PasswordlessLogin.Migrations");
                b.MigrationsHistoryTable("__PasswordlessMigrationsHistory", schema);
            });

            var config = new PasswordlessDatabaseConfig()
            { 
                Schema = schema
            };
            return new PasswordlessLoginDbContext(optionsBuilder.Options, config);
        }
    }
}