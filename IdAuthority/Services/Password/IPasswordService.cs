// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Services.Password
{
    public interface IPasswordService : IReadOnlyPasswordService
    {
        bool CanSetPassword { get; }
        bool CanChangePassword { get; }
        bool CanRemovePassword { get; }

        Task<SetPasswordResult> SetPasswordAsync(string uniqueIdentifier, string password);
        Task<ChangePasswordResult> ChangePasswordAsync(string uniqueIdentifier, string oldPassword, string newPassword);
        Task<RemovePasswordResult> RemovePasswordAsync(string uniqueIdentifier);
    }
}
