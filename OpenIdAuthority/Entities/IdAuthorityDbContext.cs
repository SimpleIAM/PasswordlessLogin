// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;

namespace SimpleIAM.OpenIdAuthority.Entities
{
    public class OpenIdAuthorityDbContext : DbContext
    {
        public OpenIdAuthorityDbContext(DbContextOptions<OpenIdAuthorityDbContext> options)
            : base(options)
        { }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<OneTimeCode> OneTimeCodes { get; set; }
        public DbSet<PasswordHash> PasswordHashes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subject>(subject =>
            {
                subject.HasKey(x => x.SubjectId);

                subject.Property(x => x.SubjectId).HasMaxLength(36).IsRequired();
                subject.Property(x => x.Email).HasMaxLength(254).IsRequired();

                subject.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<OneTimeCode>(otc =>
            {
                otc.HasKey(x => x.SentTo);

                otc.Property(x => x.SentTo).HasMaxLength(254).IsRequired();
                otc.Property(x => x.ShortCodeHash);
                otc.Property(x => x.LongCodeHash);
                otc.Property(x => x.ExpiresUTC).IsRequired();
                otc.Property(x => x.RedirectUrl).HasMaxLength(2048);
            });

            modelBuilder.Entity<PasswordHash>(ph =>
            {
                ph.HasKey(x => x.SubjectId);

                ph.Property(x => x.SubjectId).HasMaxLength(36).IsRequired();
                ph.Property(x => x.LastChangedUTC).IsRequired();
                ph.Property(x => x.Hash).IsRequired();
                ph.Property(x => x.FailedAttemptCount).IsRequired();
            });
        }
    }
}
