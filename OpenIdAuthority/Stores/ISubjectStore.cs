// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Entities;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public interface ISubjectStore
    {
        Task<Subject> GetSubjectAsync(string subjectId);
        Task<Subject> GetSubjectByEmailAsync(string email);
        Task<Subject> AddSubjectAsync(Subject subject);
    }
}
