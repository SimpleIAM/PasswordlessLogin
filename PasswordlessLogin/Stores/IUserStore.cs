// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using StandardResponse;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface IUserStore
    {
        Task<Response<User, Status>> GetUserAsync(string subjectId, bool fetchClaims = false);
        Task<Response<User, Status>> GetUserByEmailAsync(string email, bool fetchClaims = false);
        Task<Response<User, Status>> GetUserByUsernameAsync(string username, bool fetchClaims = false);
        Task<Response<User, Status>> GetUserByPreviousEmailAsync(string previousEmail);
        Task<Response<User, Status>> AddUserAsync(User user);
        Task<Response<User, Status>> PatchUserAsync(string subjectId, ILookup<string, string> Properties, bool changeProtectedClaims = false);
        Task<bool> UserExists(string username);
        Task<bool> UsernameIsAvailable(string username);
    }
}
