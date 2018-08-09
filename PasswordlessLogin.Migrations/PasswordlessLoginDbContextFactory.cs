// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimpleIAM.PasswordlessLogin.Entities;

namespace SimpleIAM.PasswordlessLogin.Migrations
{
    public class PasswordlessLoginDbContextFactory : IDesignTimeDbContextFactory<PasswordlessLoginDbContext>
    {
        public PasswordlessLoginDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PasswordlessLoginDbContext>();

            var connection = "(none)";

            optionsBuilder.UseSqlServer(connection, b => b.MigrationsAssembly("SimpleIAM.PasswordlessLogin.Migrations"));

            return new PasswordlessLoginDbContext(optionsBuilder.Options);
        }
    }
}