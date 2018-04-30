// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.IdAuthority.Entities;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Services.OTC
{
    public interface IOneTimeCodeService
    {
        Task<OneTimeCode> CreateOneTimeCodeAsync(string email, TimeSpan validity, string redirectUrl = null);
        Task<OneTimeCode> UseOneTimeLinkAsync(string linkCode);
        Task<OneTimeCode> UseOneTimeCodeAsync(string email);
    }
}
