// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Stores;
using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services
{
    public class AuthorizedDeviceService : IAuthorizedDeviceService
    {
        private readonly IAuthorizedDeviceStore _authorizedDeviceStore;
        private readonly IUserStore _userStore;

        public AuthorizedDeviceService(
            IAuthorizedDeviceStore authorizedDeviceStore,
            IUserStore userStore
            )
        {
            _authorizedDeviceStore = authorizedDeviceStore;
            _userStore = userStore;
        }

        public async Task<string> AuthorizeDevice(string username, string deviceId = null, string deviceDescription = null)
        {
            var user = await _userStore.GetUserByEmailAsync(username);
            if (user == null)
            {
                return null;
            }

            // todo: Review. Accepting an existing device id opens an attack vector for pre-installing
            // cookies on a device or via a malicious browser extension. May want to have a per-user
            // device id that is stored in a DeviceId_[UniquUserSuffix] cookie
            if (deviceId == null || !(new Regex(@"^[0-9]{10,30}$").IsMatch(deviceId)))
            {
                var rngProvider = new RNGCryptoServiceProvider();
                var byteArray = new byte[8];

                rngProvider.GetBytes(byteArray);
                var deviceIdUInt = BitConverter.ToUInt64(byteArray, 0);
                deviceId = deviceIdUInt.ToString();
            }

            var result = await _authorizedDeviceStore.AddAuthorizedDeviceAsync(user.SubjectId, deviceId, deviceDescription);
            return deviceId;
        }

        public async Task<bool> DeviceIsAuthorized(string username, string deviceId)
        {
            var user = await _userStore.GetUserByEmailAsync(username);
            if (user == null)
            {
                return false;
            }
            var result = await _authorizedDeviceStore.GetAuthorizedDeviceAsync(user.SubjectId, deviceId);
            return result != null;
        }
    }
}
