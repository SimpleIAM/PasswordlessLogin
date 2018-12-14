// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Entities;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.EventNotification
{
    public class DefaultEventNotificationService : IEventNotificationService
    {
        private readonly PasswordlessLoginDbContext _context;

        public DefaultEventNotificationService(PasswordlessLoginDbContext context)
        {
            _context = context;
        }

        public async Task NotifyEventAsync(string username, EventType eventType, string details = null)
        {
            var entry = new EventLog()
            {
                Time = DateTime.UtcNow,
                Username = username,
                EventType = eventType.ToString(),
                Details = details
            };
            _context.Add(entry);
            await _context.SaveChangesAsync();
        }
    }
}
