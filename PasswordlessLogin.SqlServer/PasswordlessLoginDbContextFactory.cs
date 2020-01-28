// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimpleIAM.PasswordlessLogin.Entities;

namespace SimpleIAM.PasswordlessLogin.SqlServer
{
    public class PasswordlessLoginDbContextFactory : IDesignTimeDbContextFactory<SqlServerPasswordlessLoginDbContext>
    {
        public SqlServerPasswordlessLoginDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PasswordlessLoginDbContext>();
            var config = new SqlServerPasswordlessDatabaseConfig();

            var connection = "(none)";            

            optionsBuilder.UseSqlServer(connection, b =>
            {
                b.MigrationsAssembly("SimpleIAM.PasswordlessLogin.SqlServer");
                b.MigrationsHistoryTable("__PasswordlessMigrationsHistory", config.Schema);
            });
            
            return new SqlServerPasswordlessLoginDbContext(optionsBuilder.Options, config);
        }
    }
}