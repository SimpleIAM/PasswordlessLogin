// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using SimpleIAM.IdAuthority.Entities;

namespace SimpleIAM.IdAuthority.Stores
{
    public class DbPasswordHashStore : IPasswordHashStore
    {
        private IdAuthorityDbContext _context;

        public DbPasswordHashStore(IdAuthorityDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddPasswordHashAsync(string uniqueIdentifier, string passwordHash)
        {
            var record = new PasswordHash()
            {
                SubjectId = uniqueIdentifier,
                Hash = passwordHash,
                LastChangedUTC = DateTime.UtcNow,
                FailedAttemptCount = 0,
                TempLockUntilUTC = null
            };
            await _context.AddAsync(record);
            var count = await _context.SaveChangesAsync();
            return count > 0;
        }

        public async Task<Models.PasswordHash> GetPasswordHashAsync(string uniqueIdentifier)
        {
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            return record?.ToModel();
        }

        public async Task<bool> UpdatePasswordHashFailureAsync(string uniqueIdentifier, int failureCount)
        {
            var count = 0;
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if(record != null)
            {
                record.FailedAttemptCount = failureCount;
                _context.Entry(record).Property(x => x.Hash).IsModified = false;
                _context.Entry(record).Property(x => x.LastChangedUTC).IsModified = false;
                _context.Entry(record).Property(x => x.TempLockUntilUTC).IsModified = false;
                count = await _context.SaveChangesAsync();
            }
            return count > 0;
        }

        public async Task<bool> RemovePasswordHashAsync(string uniqueIdentifier)
        {
            var count = 1;
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if (record != null)
            {
                _context.Remove(record);
                count = await _context.SaveChangesAsync();
            }
            return count > 0;
        }

        public async Task<bool> TempLockPasswordHashAsync(string uniqueIdentifier, DateTime lockUntil)
        {
            var count = 0;
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if (record != null)
            {
                record.TempLockUntilUTC = lockUntil;
                _context.Entry(record).Property(x => x.Hash).IsModified = false;
                _context.Entry(record).Property(x => x.LastChangedUTC).IsModified = false;
                _context.Entry(record).Property(x => x.FailedAttemptCount).IsModified = false;
                count = await _context.SaveChangesAsync();
            }
            return count > 0;
        }

        public async Task<bool> UpdatePasswordHashAsync(string uniqueIdentifier, string newHash)
        {
            var count = 0;
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if (record != null)
            {
                record.Hash = newHash;
                _context.Entry(record).Property(x => x.LastChangedUTC).IsModified = false;
                _context.Entry(record).Property(x => x.FailedAttemptCount).IsModified = false;
                _context.Entry(record).Property(x => x.TempLockUntilUTC).IsModified = false;
                count = await _context.SaveChangesAsync();
            }
            return count > 0;
        }
    }
}
