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
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public class DbAuthorizedDeviceStore : IAuthorizedDeviceStore
    {
        private readonly ILogger _logger;
        private readonly PasswordlessLoginDbContext _context;

        public DbAuthorizedDeviceStore(ILogger<DbAuthorizedDeviceStore> logger, PasswordlessLoginDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Response<Models.AuthorizedDevice, Status>> AddAuthorizedDeviceAsync(string subjectId, string deviceId, string description)
        {
            _logger.LogDebug("Adding authorized device");

            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(deviceId))
            {
                _logger.LogDebug("Subject id or device id was missing.");
                return Response.Error<Models.AuthorizedDevice>("Subject id or device id was missing.");
            }

            var dbDevice = new AuthorizedDevice()
            {
                SubjectId = subjectId,
                DeviceIdHash = FastHashService.GetHash(deviceId, subjectId),
                Description = description?.Substring(0, description.Length),
                AddedOn = DateTime.UtcNow,
            };

            await _context.AddAsync(dbDevice);
            var count = await _context.SaveChangesAsync();
            if(count == 0)
            {
                return Response.Error<Models.AuthorizedDevice>("Failed to save trusted browser.");
            }

            return Response.Success(dbDevice.ToModel(), "Trusted browser saved."); ;
        }

        public async Task<Response<Models.AuthorizedDevice, Status>> GetAuthorizedDeviceAsync(string subjectId, string deviceId)
        {
            _logger.LogDebug("Looking for authorized device");

            if (string.IsNullOrEmpty(subjectId) || string.IsNullOrEmpty(deviceId))
            {
                _logger.LogDebug("Subject id or device id was missing");
                return Response.Error<Models.AuthorizedDevice>("Subject id or device id was missing");
            }
            var deviceIdHash = FastHashService.GetHash(deviceId, subjectId);
            var model = (await _context.AuthorizedDevices.SingleOrDefaultAsync(x => x.DeviceIdHash == deviceIdHash))?.ToModel();
            if (model == null)
            {
                _logger.LogDebug("Authorized device was not found.");
                return Response.Error<Models.AuthorizedDevice>("Trusted browser was not found.");
            }
            _logger.LogDebug("Authorized device was found");
            return Response.Success(model, "Trusted browser found.");
        }

        public async Task<Response<IEnumerable<Models.AuthorizedDevice>, Status>> GetAuthorizedDevicesAsync(string subjectId)
        {
            _logger.LogDebug("Looking for authorized devices");

            if (string.IsNullOrEmpty(subjectId))
            {
                return Response.Error<IEnumerable<Models.AuthorizedDevice>>("Subject id was missing.");
            }
            var devices = await _context.AuthorizedDevices.Where(x => x.SubjectId == subjectId).Select(x => x.ToModel()).ToListAsync();
            return Response.Success((IEnumerable<Models.AuthorizedDevice>)devices);
        }

        public async Task<Status> RemoveAuthorizedDeviceAsync(string subjectId, int recordId)
        {
            _logger.LogDebug("Removing an authorized device");

            var dbDevice = await _context.AuthorizedDevices.FindAsync(recordId);
            if(dbDevice == null || dbDevice.SubjectId != subjectId)
            {
                _logger.LogDebug("Device not found or it did not belong to this user");
                return Status.Error("Trusted browser not found or it did not belong to this user.");
            }
            _context.Remove(dbDevice);
            var count = await _context.SaveChangesAsync();
            var success = count > 0;
            _logger.LogDebug("Authorized device successfully removed.");
            return Status.Success("Trusted browser successfully removed.");
        }
    }
}
