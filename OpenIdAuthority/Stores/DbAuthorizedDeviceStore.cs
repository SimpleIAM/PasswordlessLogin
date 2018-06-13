// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleIAM.OpenIdAuthority.Entities;
using SimpleIAM.OpenIdAuthority.Services;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public class DbAuthorizedDeviceStore : IAuthorizedDeviceStore
    {
        private OpenIdAuthorityDbContext _context;

        public DbAuthorizedDeviceStore(OpenIdAuthorityDbContext context)
        {
            _context = context;
        }

        public async Task<Models.AuthorizedDevice> AddAuthorizedDeviceAsync(string subjectId, string deviceId, string description)
        {
            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(deviceId))
            {
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
            if(string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(deviceId))
            {
                return null;
            }
            var deviceIdHash = FastHashService.GetHash(deviceId, subjectId);
            return (await _context.AuthorizedDevices.SingleOrDefaultAsync(x => x.DeviceIdHash == deviceIdHash))?.ToModel();
        }

        public async Task<IEnumerable<Models.AuthorizedDevice>> GetAuthorizedDevicesAsync(string subjectId)
        {
            if(string.IsNullOrEmpty(subjectId))
            {
                return new List<Models.AuthorizedDevice>();
            }
            return await _context.AuthorizedDevices.Where(x => x.SubjectId == subjectId).Select(x => x.ToModel()).ToListAsync();
        }

        public async Task<bool> RemoveAuthorizedDeviceAsync(string subjectId, int recordId)
        {
            var dbDevice = await _context.AuthorizedDevices.FindAsync(recordId);
            if(dbDevice == null || dbDevice.SubjectId != subjectId)
            {
                return false;
            }
            _context.Remove(dbDevice);
            var count = await _context.SaveChangesAsync();
            return count > 0;
        }
    }
}
