// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimpleIAM.IdAuthority.Entities;

namespace SimpleIAM.IdAuthority.Migrations
{
    public class IdAuthorityDbContextFactory : IDesignTimeDbContextFactory<IdAuthorityDbContext>
    {
        public IdAuthorityDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IdAuthorityDbContext>();

            var connection = "(none)";

            optionsBuilder.UseSqlServer(connection, b => b.MigrationsAssembly("SimpleIAM.IdAuthority.Migrations"));

            return new IdAuthorityDbContext(optionsBuilder.Options);
        }
    }
}