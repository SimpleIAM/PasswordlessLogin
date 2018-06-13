// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public interface IAuthorizedDeviceStore
    {
        Task<AuthorizedDevice> AddAuthorizedDeviceAsync(string subjectId, string deviceId, string description);
        Task<AuthorizedDevice> GetAuthorizedDeviceAsync(string subjectId, string deviceId);
        Task<bool> RemoveAuthorizedDeviceAsync(string subjectId, int recordId);
        Task<IEnumerable<AuthorizedDevice>> GetAuthorizedDevicesAsync(string subjectId);
    }
}
