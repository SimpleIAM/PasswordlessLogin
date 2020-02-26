// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Entities;
using StandardResponse;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.EventNotification
{
    public class DefaultEventNotificationService : IEventNotificationService
    {
        // Note: status messages from this service are not localized because they are not intended for end users

        private readonly PasswordlessLoginDbContext _context;

        public DefaultEventNotificationService(PasswordlessLoginDbContext context)
        {
            _context = context;
        }

        public async Task<Status> NotifyEventAsync(string username, string eventType, string details = null)
        {
            var entry = new EventLog()
            {
                Time = DateTime.UtcNow,
                Username = username,
                EventType = eventType,
                Details = details?.Substring(0, Math.Min(details.Length, 255)) // max 255 characters in this implementation
            };
            _context.Add(entry);
            var count = await _context.SaveChangesAsync();
            if(count == 0)
            {
                return Status.Error("Failed to save notification.");
            }
            return Status.Success("Notification saved.");
        }
    }
}
