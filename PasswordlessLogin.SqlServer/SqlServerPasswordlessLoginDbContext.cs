// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using SimpleIAM.PasswordlessLogin.Entities;

namespace SimpleIAM.PasswordlessLogin.SqlServer
{
    public class SqlServerPasswordlessLoginDbContext : PasswordlessLoginDbContext
    {
        private readonly string _schema;

        // NOTE: Using type of DbContextOptions<PasswordlessLoginDbContext> for options so that it can be passed to the base class.
        public SqlServerPasswordlessLoginDbContext(DbContextOptions<PasswordlessLoginDbContext> options, SqlServerPasswordlessDatabaseConfig config = null)
            : base(options)
        {
            _schema = (config ?? new SqlServerPasswordlessDatabaseConfig())?.Schema;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (_schema != null)
            {
                modelBuilder.HasDefaultSchema(_schema);
            }
            base.OnModelCreating(modelBuilder);
        }
    }
}