// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface IUserStore
    {
        Task<User> GetUserAsync(string subjectId, bool fetchClaims = false);
        Task<User> GetUserByEmailAsync(string email, bool fetchClaims = false);
        Task<User> GetUserByPreviousEmailAsync(string previousEmail);
        Task<User> AddUserAsync(User user);
        Task<User> PatchUserAsync(string subjectId, ILookup<string, string> Properties, bool changeProtectedClaims = false);
        Task<bool> UserExists(string email);
        Task<bool> UsernameIsAvailable(string email);
    }
}
