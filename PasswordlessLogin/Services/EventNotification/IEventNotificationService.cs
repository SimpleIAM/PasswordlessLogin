// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.EventNotification
{
    public interface IEventNotificationService
    {
        Task NotifyEventAsync(string username, EventType eventType, string details = null);
    }
}
