// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using StandardResponse;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface IPasswordHashStore
    {
        Task<Response<PasswordHash, Status>> GetPasswordHashAsync(string uniqueIdentifier);
        Task<Status> AddPasswordHashAsync(string uniqueIdentifier, string hash);
        Task<Status> UpdatePasswordHashAsync(string uniqueIdentifier, string newHash);
        Task<Status> UpdatePasswordHashFailureCountAsync(string uniqueIdentifier, int failureCount);
        Task<Status> UpdatePasswordHashTempLockAsync(string uniqueIdentifier, DateTime? lockUntil, int failureCount);
        Task<Status> RemovePasswordHashAsync(string uniqueIdentifier);
    }
}
