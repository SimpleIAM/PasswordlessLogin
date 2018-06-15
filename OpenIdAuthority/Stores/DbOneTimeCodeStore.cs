// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleIAM.OpenIdAuthority.Entities;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public class DbOneTimeCodeStore : IOneTimeCodeStore
    {
        private readonly ILogger _logger;
        private readonly OpenIdAuthorityDbContext _context;

        public DbOneTimeCodeStore(ILogger<DbOneTimeCodeStore> logger, OpenIdAuthorityDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Models.OneTimeCode> GetOneTimeCodeAsync(string sentTo)
        {
            _logger.LogTrace("Fetching the one time code for {0}", sentTo);
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            var model = record?.ToModel();
            _logger.LogDebug(model == null ? "OTC not found" : "OTC found");
            return model;
        }

        public async Task<Models.OneTimeCode> GetOneTimeCodeByLongCodeAsync(string longCodeHash)
        {
            _logger.LogTrace("Fetching the one time code matching the long code hash");
            var record = await _context.OneTimeCodes.SingleOrDefaultAsync(x => x.LongCode == longCodeHash);
            var model = record?.ToModel();
            _logger.LogDebug(model == null ? "OTC not found" : "OTC found");
            return model;
        }

        public async Task<bool> AddOneTimeCodeAsync(Models.OneTimeCode oneTimeCode)
        {
            _logger.LogTrace("Persisting one time code for {0}", oneTimeCode.SentTo);
            var record = oneTimeCode.ToEntity();
            await _context.AddAsync(record);
            var count = await _context.SaveChangesAsync();
            var success = count > 0;
            _logger.LogDebug("{0}uccessfully persisting one time code", success ? "S" : "Uns");
            return success;
        }

        public async Task<bool> UpdateOneTimeCodeSentCountAsync(string sentTo, int sentCount, string newRedirectUrl = null)
        {
            _logger.LogTrace("Update one time code sent count to {0}", sentCount);
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
            var success = count > 0;
            _logger.LogTrace("{0}uccessfully updated one time code sent count", success ? "S" : "Uns");
            return success;
        }

        public async Task<bool> UpdateOneTimeCodeFailureAsync(string sentTo, int failureCount)
        {
            _logger.LogTrace("Update one time code failure count to {0}", failureCount);
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
            var success = count > 0;
            _logger.LogTrace("{0}uccessfully updated one time code failure count", success ? "S" : "Uns");
            return success;
        }

        public async Task<bool> ExpireOneTimeCodeAsync(string sentTo)
        {
            _logger.LogTrace("Expiring one time code for {0}", sentTo);
            var count = 0;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                record.ExpiresUTC = DateTime.UtcNow;
                count = await _context.SaveChangesAsync();
            }
            var success = count > 0;
            _logger.LogTrace("{0}uccessfully expired one time code", success ? "S" : "Uns");
            return success;
        }

        public async Task<bool> RemoveOneTimeCodeAsync(string sentTo)
        {
            _logger.LogTrace("Removing one time code for {0}", sentTo);
            var count = 1;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                _context.Remove(record);
                count = await _context.SaveChangesAsync();
            }
            var success = count > 0;
            _logger.LogTrace("{0}uccessfully removed one time code", success ? "S" : "Uns");
            return success;
        }
    }
}
