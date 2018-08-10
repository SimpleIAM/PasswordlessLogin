// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public interface ISignInService
    {
        Task SignInAsync(string subjectId, string username, AuthenticationProperties authProps);
        Task SignOutAsync(string subjectId, string username);
    }
}
