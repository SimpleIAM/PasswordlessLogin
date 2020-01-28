// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;
using SimpleIAM.PasswordlessLogin.Configuration;

namespace SimpleIAM.PasswordlessLogin.Entities
{
    public class PasswordlessLoginDbContext : DbContext
    {
        public PasswordlessLoginDbContext(DbContextOptions<PasswordlessLoginDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserClaim> Claims { get; set; }
        public DbSet<OneTimeCode> OneTimeCodes { get; set; }
        public DbSet<PasswordHash> PasswordHashes { get; set; }
        public DbSet<TrustedBrowser> TrustedBrowsers { get; set; }
        public DbSet<EventLog> EventLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(user =>
            {
                user.HasKey(x => x.SubjectId);

                user.Property(x => x.SubjectId).HasMaxLength(36).IsRequired();
                user.Property(x => x.Email).HasMaxLength(254).IsRequired();
                user.Property(x => x.CreatedUTC).IsRequired();

                user.HasIndex(x => x.Email).IsUnique();

                user.HasMany(x => x.Claims).WithOne().HasForeignKey(x => x.SubjectId);
            });

            modelBuilder.Entity<UserClaim>(uc =>
            {
                uc.HasKey(x => x.Id);

                uc.Property(x => x.SubjectId).HasMaxLength(36).IsRequired();
                uc.Property(x => x.Type).HasMaxLength(255).IsRequired();
                uc.Property(x => x.Value).HasMaxLength(4000).IsRequired();

                uc.HasIndex(x => x.SubjectId);
                uc.HasIndex(x => x.Type);
            });

            modelBuilder.Entity<OneTimeCode>(otc =>
            {
                otc.HasKey(x => x.SentTo);

                otc.Property(x => x.SentTo).HasMaxLength(254).IsRequired();
                otc.Property(x => x.ShortCode);
                otc.Property(x => x.LongCode);
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

            modelBuilder.Entity<TrustedBrowser>(tb =>
            {
                tb.HasKey(x => x.Id);

                tb.Property(x => x.SubjectId).HasMaxLength(36).IsRequired();
                tb.Property(x => x.BrowserIdHash).IsRequired();
                tb.Property(x => x.Description);
                tb.Property(x => x.AddedOn).IsRequired();
            });

            modelBuilder.Entity<EventLog>(el =>
            {
                el.HasKey(x => x.Id);

                el.Property(x => x.Time).IsRequired();
                el.Property(x => x.Username).HasMaxLength(254);
                el.Property(x => x.EventType).HasMaxLength(30).IsRequired();
                el.Property(x => x.Details).HasMaxLength(255);
            });
        }
    }
}
