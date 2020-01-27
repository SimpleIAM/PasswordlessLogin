// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimpleIAM.PasswordlessLogin.Entities;

namespace SimpleIAM.PasswordlessLogin.MySql
{
    public class PasswordlessLoginDbContextFactory : IDesignTimeDbContextFactory<PasswordlessLoginDbContext>
    {
        public PasswordlessLoginDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PasswordlessLoginDbContext>();

            var connection = "Server=none;Database=none;User=none;Password=none;";

            optionsBuilder.UseMySql(connection, b =>
            {
                b.MigrationsAssembly("SimpleIAM.PasswordlessLogin.MySql");
                b.MigrationsHistoryTable("__PasswordlessMigrationsHistory");
            });

            return new PasswordlessLoginDbContext(optionsBuilder.Options);
        }
    }
}