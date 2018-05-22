// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.OTC
{
    public interface IOneTimeCodeService
    {
        Task<GetOneTimeCodeResponse> GetOneTimeCodeAsync(string sendTo, TimeSpan validity, string redirectUrl = null);
        Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string longCode);
        Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string sentTo, string shortCode);
    }
}
