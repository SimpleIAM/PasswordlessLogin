// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface IPasswordHashStore
    {
        Task<Response<PasswordHash>> GetPasswordHashAsync(string uniqueIdentifier);
        Task<Status> AddPasswordHashAsync(string uniqueIdentifier, string hash);
        Task<Status> UpdatePasswordHashAsync(string uniqueIdentifier, string newHash);
        Task<Status> UpdatePasswordHashFailureCountAsync(string uniqueIdentifier, int failureCount);
        Task<Status> TempLockPasswordHashAsync(string uniqueIdentifier, DateTime lockUntil, int failureCount);
        Task<Status> RemovePasswordHashAsync(string uniqueIdentifier);
    }
}
