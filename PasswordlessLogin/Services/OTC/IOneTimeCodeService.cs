// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using StandardResponse;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public interface IOneTimeCodeService
    {
        Task<Response<GetOneTimeCodeResult, GetOneTimeCodeStatus>> GetOneTimeCodeAsync(string sendTo, TimeSpan validity, string redirectUrl = null);
        Task<Response<CheckOneTimeCodeResult, CheckOneTimeCodeStatus>> CheckOneTimeCodeAsync(string longCode, string clientNonce);
        Task<Response<CheckOneTimeCodeResult, CheckOneTimeCodeStatus>> CheckOneTimeCodeAsync(string sentTo, string shortCode, string clientNonce);
        Task<bool> UnexpiredOneTimeCodeExistsAsync(string sentTo);
    }
}
