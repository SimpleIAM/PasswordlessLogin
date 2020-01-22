// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public interface IReadOnlyPasswordService
    {
        Task<CheckPasswordStatus> CheckPasswordAsync(string uniqueIdentifier, string password);
    }
}
