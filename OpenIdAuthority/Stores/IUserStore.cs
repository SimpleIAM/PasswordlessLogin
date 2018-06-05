// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Models;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public interface IUserStore
    {
        Task<User> GetUserAsync(string subjectId, bool fetchClaims = false);
        Task<User> GetUserByEmailAsync(string email, bool fetchClaims = false);
        Task<User> AddUserAsync(User user);
    }
}
