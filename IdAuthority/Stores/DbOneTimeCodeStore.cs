// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleIAM.IdAuthority.Entities;

namespace SimpleIAM.IdAuthority.Stores
{
    public class DbOneTimeCodeStore : IOneTimeCodeStore
    {
        private IdAuthorityDbContext _context;

        public DbOneTimeCodeStore(IdAuthorityDbContext context)
        {
            _context = context;
        }

        public async Task<Models.OneTimeCode> GetOneTimeCodeAsync(string sentTo)
        {
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            return record?.ToModel();
        }

        public async Task<Models.OneTimeCode> GetOneTimeCodeByLongCodeAsync(string longCodeHash)
        {
            var record = await _context.OneTimeCodes.SingleOrDefaultAsync(x => x.LongCodeHash == longCodeHash);
            return record?.ToModel();
        }

        public async Task<bool> AddOneTimeCodeAsync(Models.OneTimeCode oneTimeCode)
        {
            var record = oneTimeCode.ToEntity();
            await _context.AddAsync(record);
            var count = await _context.SaveChangesAsync();
            return count > 0;

        }

        public async Task<bool> UpdateOneTimeCodeFailureAsync(string sentTo, int failureCount)
        {
            var count = 0;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                record.FailedAttemptCount = failureCount;
                _context.Entry(record).Property(x => x.ExpiresUTC).IsModified = false;
                _context.Entry(record).Property(x => x.LongCodeHash).IsModified = false;
                _context.Entry(record).Property(x => x.RedirectUrl).IsModified = false;
                count = await _context.SaveChangesAsync();
            }
            return count > 0;
        }

        public async Task<bool> ExpireOneTimeCodeAsync(string sentTo)
        {
            var count = 0;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                record.ExpiresUTC = DateTime.UtcNow;
                count = await _context.SaveChangesAsync();
            }
            return count > 0;
        }

        public async Task<bool> RemoveOneTimeCodeAsync(string sentTo)
        {
            var count = 1;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                _context.Remove(record);
                count = await _context.SaveChangesAsync();
            }
            return count > 0;
        }
    }
}
