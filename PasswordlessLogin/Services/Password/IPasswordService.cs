// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using StandardResponse;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public interface IPasswordService : IReadOnlyPasswordService
    {
        Task<Response<DateTime, Status>> PasswordLastChangedAsync(string uniqueIdentifier);
        Task<SetPasswordStatus> SetPasswordAsync(string uniqueIdentifier, string password);
        Task<Status> RemovePasswordAsync(string uniqueIdentifier);
    }
}
