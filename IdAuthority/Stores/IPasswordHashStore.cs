// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.IdAuthority.Models;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Stores
{
    public interface IPasswordHashStore
    {
        Task<PasswordHash> GetPasswordHashAsync(string uniqueIdentifier);
        Task<bool> AddPasswordHashAsync(string uniqueIdentifier, string hash);
        Task<bool> UpdatePasswordHashAsync(string uniqueIdentifier, string newHash);
        Task<bool> UpdatePasswordHashFailureAsync(string uniqueIdentifier, int failureCount);
        Task<bool> TempLockPasswordHashAsync(string uniqueIdentifier, DateTime lockUntil);
        Task<bool> RemovePasswordHashAsync(string uniqueIdentifier);
    }
}
