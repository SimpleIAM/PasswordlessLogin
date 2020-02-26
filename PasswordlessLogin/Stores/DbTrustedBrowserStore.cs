// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Entities;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Services.Localization;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public class DbTrustedBrowserStore : ITrustedBrowserStore
    {
        private readonly IApplicationLocalizer _localizer;
        private readonly ILogger _logger;
        private readonly PasswordlessLoginDbContext _context;

        public DbTrustedBrowserStore(IApplicationLocalizer localizer, ILogger<DbTrustedBrowserStore> logger, PasswordlessLoginDbContext context)
        {
            _localizer = localizer;
            _logger = logger;
            _context = context;
        }

        public async Task<Response<Models.TrustedBrowser, Status>> AddTrustedBrowserAsync(string subjectId, string browserId, string description)
        {
            _logger.LogDebug("Adding trusted browser");

            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(browserId))
            {
                _logger.LogDebug("Subject id or browser id was missing.");
                return Response.Error<Models.TrustedBrowser>(_localizer["Subject id or browser id was missing."]);
            }

            var dbBrowser = new TrustedBrowser()
            {
                SubjectId = subjectId,
                BrowserIdHash = FastHashService.GetHash(browserId, subjectId),
                Description = description?.Substring(0, description.Length),
                AddedOn = DateTime.UtcNow,
            };

            await _context.AddAsync(dbBrowser);
            var count = await _context.SaveChangesAsync();
            if(count == 0)
            {
                return Response.Error<Models.TrustedBrowser>(_localizer["Failed to save trusted browser."]);
            }

            return Response.Success(dbBrowser.ToModel(), _localizer["Trusted browser saved."]);
        }

        public async Task<Response<Models.TrustedBrowser, Status>> GetTrustedBrowserAsync(string subjectId, string browserId)
        {
            _logger.LogDebug("Looking for trusted browser.");

            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(browserId))
            {
                _logger.LogDebug("Subject id or browser id was missing.");
                return Response.Error<Models.TrustedBrowser>(_localizer["Subject id or browser id was missing."]);
            }
            var browserIdHash = FastHashService.GetHash(browserId, subjectId);
            var model = (await _context.TrustedBrowsers.SingleOrDefaultAsync(x => x.BrowserIdHash == browserIdHash))?.ToModel();
            if (model == null)
            {
                _logger.LogDebug("Trusted browser was not found.");
                return Response.Error<Models.TrustedBrowser>(_localizer["Trusted browser was not found."]);
            }
            _logger.LogDebug("Trusted browser was found.");
            return Response.Success(model, _localizer["Trusted browser found."]);
        }

        public async Task<Response<IEnumerable<Models.TrustedBrowser>, Status>> GetTrustedBrowserAsync(string subjectId)
        {
            _logger.LogDebug("Looking for trusted browsers.");

            if (string.IsNullOrEmpty(subjectId))
            {
                return Response.Error<IEnumerable<Models.TrustedBrowser>>(_localizer["Subject id was missing."]);
            }
            var browsers = await _context.TrustedBrowsers.Where(x => x.SubjectId == subjectId).Select(x => x.ToModel()).ToListAsync();
            return Response.Success((IEnumerable<Models.TrustedBrowser>)browsers);
        }

        public async Task<Status> RemoveTrustedBrowserAsync(string subjectId, int recordId)
        {
            _logger.LogDebug("Removing an trusted browser.");

            var dbBrowser = await _context.TrustedBrowsers.FindAsync(recordId);
            if(dbBrowser == null || dbBrowser.SubjectId != subjectId)
            {
                _logger.LogDebug("Trusted browser not found or it did not belong to this user.");
                return Status.Error(_localizer["Trusted browser not found or it did not belong to this user."]);
            }
            _context.Remove(dbBrowser);
            var count = await _context.SaveChangesAsync();
            var success = count > 0;
            _logger.LogDebug("Trusted browser successfully removed.");
            return Status.Success(_localizer["Trusted browser successfully removed."]);
        }
    }
}
