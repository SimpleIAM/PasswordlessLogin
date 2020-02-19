// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using StandardResponse;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.EventNotification
{
    public interface IEventNotificationService
    {
        Task<Status> NotifyEventAsync(string username, string eventType, string details = null);
    }
}
