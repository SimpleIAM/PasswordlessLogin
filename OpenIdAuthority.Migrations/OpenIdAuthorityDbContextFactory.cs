// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimpleIAM.OpenIdAuthority.Entities;

namespace SimpleIAM.OpenIdAuthority.Migrations
{
    public class OpenIdAuthorityDbContextFactory : IDesignTimeDbContextFactory<OpenIdAuthorityDbContext>
    {
        public OpenIdAuthorityDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OpenIdAuthorityDbContext>();

            var connection = "(none)";

            optionsBuilder.UseSqlServer(connection, b => b.MigrationsAssembly("SimpleIAM.OpenIdAuthority.Migrations"));

            return new OpenIdAuthorityDbContext(optionsBuilder.Options);
        }
    }
}