// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services
{
    public interface IAuthorizedDeviceService
    {
        Task<string> AuthorizeDevice(string username, string deviceId = null, string deviceDescription = null);
        Task<bool> DeviceIsAuthorized(string username, string deviceId);
    }
}
