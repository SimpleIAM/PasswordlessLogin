// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using StandardResponse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface IAuthorizedDeviceStore
    {
        Task<Response<AuthorizedDevice, Status>> AddAuthorizedDeviceAsync(string subjectId, string deviceId, string description);
        Task<Response<AuthorizedDevice, Status>> GetAuthorizedDeviceAsync(string subjectId, string deviceId);
        Task<Status> RemoveAuthorizedDeviceAsync(string subjectId, int recordId);
        Task<Response<IEnumerable<AuthorizedDevice>, Status>> GetAuthorizedDevicesAsync(string subjectId);
    }
}
