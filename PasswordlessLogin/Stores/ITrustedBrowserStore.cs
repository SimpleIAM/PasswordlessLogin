// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using StandardResponse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface ITrustedBrowserStore
    {
        Task<Response<TrustedBrowser, Status>> AddTrustedBrowserAsync(string subjectId, string browserId, string description);
        Task<Response<TrustedBrowser, Status>> GetTrustedBrowserAsync(string subjectId, string browserId);
        Task<Status> RemoveTrustedBrowserAsync(string subjectId, int recordId);
        Task<Response<IEnumerable<TrustedBrowser>, Status>> GetTrustedBrowserAsync(string subjectId);
    }
}
