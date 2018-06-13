// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Models;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public interface IOneTimeCodeStore
    {
        Task<OneTimeCode> GetOneTimeCodeAsync(string sentTo);
        Task<OneTimeCode> GetOneTimeCodeByLongCodeAsync(string longCodeHash);
        Task<bool> AddOneTimeCodeAsync(OneTimeCode oneTimeCode);
        Task<bool> UpdateOneTimeCodeFailureAsync(string sentTo, int failureCount);
        Task<bool> UpdateOneTimeCodeSentCountAsync(string sentTo, int sentCount, string newRedirectUrl = null);
        Task<bool> ExpireOneTimeCodeAsync(string sentTo);
        Task<bool> RemoveOneTimeCodeAsync(string sentTo);
    }
}
