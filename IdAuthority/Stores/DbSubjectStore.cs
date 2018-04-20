// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleIAM.IdAuthority.Entities;

namespace SimpleIAM.IdAuthority.Stores
{
    public class DbSubjectStore : ISubjectStore
    {
        private IdAuthorityDbContext _context;

        public DbSubjectStore(IdAuthorityDbContext context)
        {
            _context = context;
        }

        public async Task<Subject> AddSubjectAsync(Subject subject)
        {
            if(subject.SubjectId == null)
            {
                subject.SubjectId = Guid.NewGuid().ToString("N");
            }
            await _context.AddAsync(subject);
            await _context.SaveChangesAsync();

            return subject;
        }

        public async Task<Subject> GetSubjectAsync(string subjectId)
        {
            return await _context.Subjects.FindAsync(subjectId);
        }

        public async Task<Subject> GetSubjectByEmailAsync(string email, bool createIfNotFound = false)
        {
            var subject = await _context.Subjects.SingleOrDefaultAsync(x => x.Email == email);
            if(subject == null && createIfNotFound)
            {
                subject = await AddSubjectAsync(new Subject() { Email = email });
            }
            return subject;
        }
    }
}
