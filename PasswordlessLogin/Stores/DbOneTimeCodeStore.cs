// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Entities;
using SimpleIAM.PasswordlessLogin.Services.Localization;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public class DbOneTimeCodeStore : IOneTimeCodeStore
    {
        private readonly IApplicationLocalizer _localizer;
        private readonly ILogger _logger;
        private readonly PasswordlessLoginDbContext _context;

        public DbOneTimeCodeStore(IApplicationLocalizer localizer, ILogger<DbOneTimeCodeStore> logger, PasswordlessLoginDbContext context)
        {
            _localizer = localizer;
            _logger = logger;
            _context = context;
        }

        public async Task<Response<Models.OneTimeCode, Status>> GetOneTimeCodeAsync(string sentTo)
        {
            _logger.LogTrace("Fetching the one time code for {0}", sentTo);
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record == null)
            {
                return Response.Error<Models.OneTimeCode>(_localizer["One time code not found."]);
            }

            return Response.Success(record?.ToModel(), _localizer["One time code found."]);
        }

        public async Task<Response<Models.OneTimeCode, Status>> GetOneTimeCodeByLongCodeAsync(string longCodeHash)
        {
            _logger.LogTrace("Fetching the one time code matching the long code hash");
            var record = await _context.OneTimeCodes.SingleOrDefaultAsync(x => x.LongCode == longCodeHash);
            if (record == null)
            {
                return Response.Error<Models.OneTimeCode, Status>(_localizer["One time code not found."]);
            }

            return Response.Success(record?.ToModel(), _localizer["One time code found."]);
        }

        public async Task<Status> AddOneTimeCodeAsync(Models.OneTimeCode oneTimeCode)
        {
            _logger.LogTrace("Persisting one time code for {0}", oneTimeCode.SentTo);
            var record = oneTimeCode.ToEntity();
            await _context.AddAsync(record);
            var count = await _context.SaveChangesAsync();
            if (count == 0)
            {
                return Status.Error(_localizer["One time code was not saved."]);
            }

            return Status.Success(_localizer["One time code saved."]);
        }

        public async Task<Status> UpdateOneTimeCodeSentCountAsync(string sentTo, int sentCount, string newRedirectUrl = null)
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
            if (count == 0)
            {
                return Status.Error(_localizer["One time code sent count was not updated."]);
            }

            return Status.Success(_localizer["One time code sent count was updated."]);
        }

        public async Task<Status> UpdateOneTimeCodeFailureAsync(string sentTo, int failureCount)
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
            if (count == 0)
            {
                return Status.Error(_localizer["One time code failure count was not updated."]);
            }

            return Status.Success(_localizer["One time code failure count was updated."]);
        }

        public async Task<Status> ExpireOneTimeCodeAsync(string sentTo)
        {
            _logger.LogTrace("Expiring one time code for {0}", sentTo);
            var count = 0;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                record.ExpiresUTC = DateTime.UtcNow;
                count = await _context.SaveChangesAsync();
            }
            if (count == 0)
            {
                return Status.Error(_localizer["One time code was not cancelled."]);
            }

            return Status.Success(_localizer["One time code was cancelled."]);
        }

        public async Task<Status> RemoveOneTimeCodeAsync(string sentTo)
        {
            _logger.LogTrace("Removing one time code for {0}", sentTo);
            var count = 1;
            var record = await _context.OneTimeCodes.FindAsync(sentTo);
            if (record != null)
            {
                _context.Remove(record);
                count = await _context.SaveChangesAsync();
            }
            if (count == 0)
            {
                return Status.Error(_localizer["One time code was not removed."]);
            }

            return Status.Success(_localizer["One time code was removed."]);
        }
    }
}
