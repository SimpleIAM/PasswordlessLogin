// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleIAM.OpenIdAuthority.Entities;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public class DbOneTimeCodeStore : IOneTimeCodeStore
    {
        private OpenIdAuthorityDbContext _context;

        public DbOneTimeCodeStore(OpenIdAuthorityDbContext context)
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
            var record = await _context.OneTimeCodes.SingleOrDefaultAsync(x => x.LongCode == longCodeHash);
            return record?.ToModel();
        }

        public async Task<bool> AddOneTimeCodeAsync(Models.OneTimeCode oneTimeCode)
        {
            var record = oneTimeCode.ToEntity();
            await _context.AddAsync(record);
            var count = await _context.SaveChangesAsync();
            return count > 0;

        }

        public async Task<bool> UpdateOneTimeCodeSentCountAsync(string sentTo, int sentCount, string newRedirectUrl = null)
        {
            var count = 0;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                record.SentCount = sentCount;
                _context.Entry(record).Property(x => x.FailedAttemptCount).IsModified = false;
                _context.Entry(record).Property(x => x.ExpiresUTC).IsModified = false;
                _context.Entry(record).Property(x => x.LongCode).IsModified = false;
                _context.Entry(record).Property(x => x.ShortCode).IsModified = false;
                _context.Entry(record).Property(x => x.ClientNonceHash).IsModified = false;
                if (newRedirectUrl != null)
                {
                    record.RedirectUrl = newRedirectUrl;
                }
                else
                {
                    _context.Entry(record).Property(x => x.RedirectUrl).IsModified = false;
                }
                count = await _context.SaveChangesAsync();
            }
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
                _context.Entry(record).Property(x => x.LongCode).IsModified = false;
                _context.Entry(record).Property(x => x.ShortCode).IsModified = false;
                _context.Entry(record).Property(x => x.ClientNonceHash).IsModified = false;
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
