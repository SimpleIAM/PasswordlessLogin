// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Services.Password
{
    public interface IReadOnlyPasswordService
    {
        string UniqueIdentifierClaimType { get; }

        Task<CheckPasswordResult> CheckPasswordAsync(string uniqueIdentifier, string password);
    }
}
