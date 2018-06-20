// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Password
{
    public interface IPasswordService : IReadOnlyPasswordService
    {
        Task<DateTime?> PasswordLastChangedAsync(string uniqueIdentifier);
        Task<SetPasswordResult> SetPasswordAsync(string uniqueIdentifier, string password);
        Task<RemovePasswordResult> RemovePasswordAsync(string uniqueIdentifier);
    }
}
