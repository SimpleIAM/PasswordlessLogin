// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleIAM.OpenIdAuthority.Entities;
using SimpleIAM.OpenIdAuthority.Services;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public class DbAuthorizedDeviceStore : IAuthorizedDeviceStore
    {
        private readonly ILogger _logger;
        private readonly OpenIdAuthorityDbContext _context;

        public DbAuthorizedDeviceStore(ILogger<DbAuthorizedDeviceStore> logger, OpenIdAuthorityDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Models.AuthorizedDevice> AddAuthorizedDeviceAsync(string subjectId, string deviceId, string description)
        {
            _logger.LogDebug("Adding authorized device");

            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(deviceId))
            {
                _logger.LogDebug("Subject id or device id was missing");
                return null;
            }

            var dbDevice = new AuthorizedDevice()
            {
                SubjectId = subjectId,
                DeviceIdHash = FastHashService.GetHash(deviceId, subjectId),
                Description = description?.Substring(0, description.Length),
                AddedOn = DateTime.UtcNow,
            };

            await _context.AddAsync(dbDevice);
            await _context.SaveChangesAsync();

            return dbDevice.ToModel();
        }

        public async Task<Models.AuthorizedDevice> GetAuthorizedDeviceAsync(string subjectId, string deviceId)
        {
            _logger.LogDebug("Looking for authorized device");

            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(deviceId))
            {
                _logger.LogDebug("Subject id or device id was missing");
                return null;
            }
            var deviceIdHash = FastHashService.GetHash(deviceId, subjectId);
            var model = (await _context.AuthorizedDevices.SingleOrDefaultAsync(x => x.DeviceIdHash == deviceIdHash))?.ToModel();
            _logger.LogDebug(model == null ? "Authorized device was not found" : "Authorized device was found");
            return model;
        }

        public async Task<IEnumerable<Models.AuthorizedDevice>> GetAuthorizedDevicesAsync(string subjectId)
        {
            _logger.LogDebug("Looking for authorized devices");

            if (string.IsNullOrEmpty(subjectId))
            {
                return new List<Models.AuthorizedDevice>();
            }
            return await _context.AuthorizedDevices.Where(x => x.SubjectId == subjectId).Select(x => x.ToModel()).ToListAsync();
        }

        public async Task<bool> RemoveAuthorizedDeviceAsync(string subjectId, int recordId)
        {
            _logger.LogDebug("Removing an authorized device");

            var dbDevice = await _context.AuthorizedDevices.FindAsync(recordId);
            if(dbDevice == null || dbDevice.SubjectId != subjectId)
            {
                _logger.LogDebug("Device not found or it did not belong to this user");
                return false;
            }
            _context.Remove(dbDevice);
            var count = await _context.SaveChangesAsync();
            var success = count > 0;
            _logger.LogDebug("Authorized device successfully removed");
            return success;
        }
    }
}
