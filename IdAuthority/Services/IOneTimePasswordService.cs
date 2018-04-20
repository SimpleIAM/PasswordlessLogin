// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.IdAuthority.Entities;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Services
{
    public interface IOneTimePasswordService
    {
        Task<OneTimePassword> CreateOneTimePasswordAsync(string email, TimeSpan validity, string redirectUrl = null);
        Task<OneTimePassword> UseOneTimeLinkAsync(string linkCode);
        Task<OneTimePassword> UseOneTimePasswordAsync(string email);
    }
}
