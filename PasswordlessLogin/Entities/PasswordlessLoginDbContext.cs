// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.EntityFrameworkCore;

namespace SimpleIAM.PasswordlessLogin.Entities
{
    public class PasswordlessLoginDbContext : DbContext
    {
        public PasswordlessLoginDbContext(DbContextOptions<PasswordlessLoginDbContext> options)
            : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserClaim> Claims { get; set; }
        public DbSet<OneTimeCode> OneTimeCodes { get; set; }
        public DbSet<PasswordHash> PasswordHashes { get; set; }
        public DbSet<AuthorizedDevice> AuthorizedDevices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(user =>
            {
                user.HasKey(x => x.SubjectId);

                user.Property(x => x.SubjectId).HasMaxLength(36).IsRequired();
                user.Property(x => x.Email).HasMaxLength(254).IsRequired();

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

            modelBuilder.Entity<AuthorizedDevice>(ad =>
            {
                ad.HasKey(x => x.Id);

                ad.Property(x => x.SubjectId).HasMaxLength(36).IsRequired();
                ad.Property(x => x.DeviceIdHash).IsRequired();
                ad.Property(x => x.Description);
                ad.Property(x => x.AddedOn).IsRequired();
            });
        }
    }
}
