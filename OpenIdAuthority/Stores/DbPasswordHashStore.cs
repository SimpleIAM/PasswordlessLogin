// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleIAM.OpenIdAuthority.Entities;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public class DbPasswordHashStore : IPasswordHashStore
    {
        private readonly ILogger _logger;
        private OpenIdAuthorityDbContext _context;

        public DbPasswordHashStore(ILogger<DbPasswordHashStore> logger, OpenIdAuthorityDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<bool> AddPasswordHashAsync(string uniqueIdentifier, string passwordHash)
        {
            _logger.LogTrace("Add password hash");
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
            var success = count > 0;
            _logger.LogDebug("{0}uccessfully persisting password hash", success ? "S" : "Uns");
            return success;
        }

        public async Task<Models.PasswordHash> GetPasswordHashAsync(string uniqueIdentifier)
        {
            _logger.LogTrace("Fecth password hash");
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            var model = record?.ToModel();
            _logger.LogDebug("Password hash was ", model != null ? "found" : "not found");
            return model;
        }

        public async Task<bool> UpdatePasswordHashFailureCountAsync(string uniqueIdentifier, int failureCount)
        {
            _logger.LogTrace("Update password hash failure count");
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
            var success = count > 0;
            _logger.LogDebug("{0}uccessfully updated password hash failure count", success ? "S" : "Uns");
            return success;
        }

        public async Task<bool> RemovePasswordHashAsync(string uniqueIdentifier)
        {
            _logger.LogTrace("Remove password hash");
            var count = 1;
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if (record != null)
            {
                _context.Remove(record);
                count = await _context.SaveChangesAsync();
            }
            var success = count > 0;
            _logger.LogDebug("{0}uccessfully removed password hash", success ? "S" : "Uns");
            return success;
        }

        public async Task<bool> TempLockPasswordHashAsync(string uniqueIdentifier, DateTime lockUntil)
        {
            _logger.LogTrace("Temp lock password hash");
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
            var success = count > 0;
            _logger.LogDebug("{0}uccessfully updated password hash temp lock", success ? "S" : "Uns");
            return success;
        }

        public async Task<bool> UpdatePasswordHashAsync(string uniqueIdentifier, string newHash)
        {
            _logger.LogTrace("Update password hash");
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
            var success = count > 0;
            _logger.LogDebug("{0}uccessfully updated password hash", success ? "S" : "Uns");
            return success;
        }
    }
}
