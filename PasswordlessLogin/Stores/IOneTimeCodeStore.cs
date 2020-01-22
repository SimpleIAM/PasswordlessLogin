// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using StandardResponse;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface IOneTimeCodeStore
    {
        Task<Response<OneTimeCode, Status>> GetOneTimeCodeAsync(string sentTo);
        Task<Response<OneTimeCode, Status>> GetOneTimeCodeByLongCodeAsync(string longCodeHash);
        Task<Status> AddOneTimeCodeAsync(OneTimeCode oneTimeCode);
        Task<Status> UpdateOneTimeCodeFailureAsync(string sentTo, int failureCount);
        Task<Status> UpdateOneTimeCodeSentCountAsync(string sentTo, int sentCount, string newRedirectUrl = null);
        Task<Status> ExpireOneTimeCodeAsync(string sentTo);
        Task<Status> RemoveOneTimeCodeAsync(string sentTo);
    }
}
