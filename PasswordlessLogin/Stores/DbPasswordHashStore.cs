// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Entities;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public class DbPasswordHashStore : IPasswordHashStore
    {
        private readonly ILogger _logger;
        private PasswordlessLoginDbContext _context;

        public DbPasswordHashStore(ILogger<DbPasswordHashStore> logger, PasswordlessLoginDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Status> AddPasswordHashAsync(string uniqueIdentifier, string passwordHash)
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
            if(count == 0)
            {
                return Status.Error("Failed to save password.");
            }

            return Status.Success("Password saved.");
        }

        public async Task<Response<Models.PasswordHash, Status>> GetPasswordHashAsync(string uniqueIdentifier)
        {
            _logger.LogTrace("Fetch password hash");
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if (record == null)
            {
                return Response.Error<Models.PasswordHash>("Password not found.");
            }
            return Response.Success(record.ToModel(), "Password found.");
        }

        public async Task<Status> UpdatePasswordHashFailureCountAsync(string uniqueIdentifier, int failureCount)
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
            if (count == 0)
            {
                return Status.Error("Password failure count not updated.");
            }

            return Status.Success("Password failure count updated.");
        }

        public async Task<Status> RemovePasswordHashAsync(string uniqueIdentifier)
        {
            _logger.LogTrace("Remove password hash");
            var count = 1;
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if (record != null)
            {
                _context.Remove(record);
                count = await _context.SaveChangesAsync();
            }
            if (count == 0)
            {
                return Status.Error("Password not removed.");
            }

            return Status.Success("Password removed.");
        }

        public async Task<Status> TempLockPasswordHashAsync(string uniqueIdentifier, DateTime lockUntil, int failureCount)
        {
            _logger.LogTrace("Temp lock password hash");
            var count = 0;
            var record = await _context.PasswordHashes.FindAsync(uniqueIdentifier);
            if (record != null)
            {
                record.TempLockUntilUTC = lockUntil;
                record.FailedAttemptCount = failureCount;
                _context.Entry(record).Property(x => x.Hash).IsModified = false;
                _context.Entry(record).Property(x => x.LastChangedUTC).IsModified = false;
                count = await _context.SaveChangesAsync();
            }
            if (count == 0)
            {
                return Status.Error("Failed to temporarily lock the account.");
            }

            return Status.Success("Account temporarily locked.");
        }

        public async Task<Status> UpdatePasswordHashAsync(string uniqueIdentifier, string newHash)
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
            if (count == 0)
            {
                return Status.Error("Password not updated.");
            }

            return Status.Success("Password updated.");
        }
    }
}
