// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.IdAuthority.Entities;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Stores
{
    public interface ISubjectStore
    {
        Task<Subject> GetSubjectAsync(string subjectId);
        Task<Subject> GetSubjectByEmailAsync(string email, bool createIfNotFound = false);
        Task<Subject> AddSubjectAsync(Subject subject);
    }
}
