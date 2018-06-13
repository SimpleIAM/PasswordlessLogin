// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleIAM.OpenIdAuthority.Entities;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public class DbUserStore : IUserStore
    {
        private OpenIdAuthorityDbContext _context;

        public DbUserStore(OpenIdAuthorityDbContext context)
        {
            _context = context;
        }

        public async Task<Models.User> AddUserAsync(Models.User user)
        {
            var dbUser = new Entities.User()
            {
                SubjectId = user.SubjectId ?? Guid.NewGuid().ToString("N"),
                Email = user.Email
            };
            dbUser.Claims = user.Claims?.Select(x => new Entities.UserClaim() { SubjectId = dbUser.SubjectId, Type = x.Type, Value = x.Value }).ToList();
            await _context.AddAsync(dbUser);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<Models.User> GetUserAsync(string subjectId, bool fetchClaims = false)
        {
            if (fetchClaims)
            {
                return (await _context.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.SubjectId == subjectId))?.ToModel();
            }
            return (await _context.Users.FindAsync(subjectId))?.ToModel();
        }

        public async Task<Models.User> GetUserByEmailAsync(string email, bool fetchClaims = false)
        {
            if (fetchClaims)
            {
                return (await _context.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Email == email))?.ToModel();
            }
            return (await _context.Users.SingleOrDefaultAsync(x => x.Email == email))?.ToModel();
        }

        public async Task<Models.User> PatchUserAsync(string subjectId, ILookup<string, string> Properties)
        {
            foreach (var values in Properties)
            {
                if (values.Key == "email")
                {
                    //todo: special processing
                }
                else
                {
                    _context.Claims.RemoveRange(_context.Claims.Where(x => x.SubjectId == subjectId && x.Type == values.Key));
                    _context.Claims.AddRange(values.Where(x => x != null).Select(x=> new Entities.UserClaim() { SubjectId = subjectId, Type = values.Key, Value = x }));
                }
            }
            //todo: condider using a transaction?
            await _context.SaveChangesAsync();
            return await GetUserAsync(subjectId);
        }
    }
}
