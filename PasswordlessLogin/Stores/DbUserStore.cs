// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Entities;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public class DbUserStore : IUserStore
    {
        private readonly ILogger _logger;
        private readonly PasswordlessLoginDbContext _context;

        public DbUserStore(ILogger<DbUserStore> logger, PasswordlessLoginDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<Models.User> AddUserAsync(Models.User user)
        {
            _logger.LogTrace("Add user {0}", user.Email);
            var dbUser = new Entities.User()
            {
                SubjectId = user.SubjectId ?? Guid.NewGuid().ToString("N"),
                Email = user.Email
            };
            dbUser.Claims = user.Claims?.Select(x => new Entities.UserClaim() { SubjectId = dbUser.SubjectId, Type = x.Type, Value = x.Value }).ToList();
            await _context.AddAsync(dbUser);
            var count = await _context.SaveChangesAsync();

            var success = count > 0;
            _logger.LogDebug("{0}uccessfully added user", success ? "S" : "Uns");
            return user;
        }

        public async Task<Models.User> GetUserAsync(string subjectId, bool fetchClaims = false)
        {
            _logger.LogTrace("Fetch user {0}", fetchClaims ? "and claims" : "without claims");
            if (fetchClaims)
            {
                return (await _context.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.SubjectId == subjectId))?.ToModel();
            }
            var model = (await _context.Users.FindAsync(subjectId))?.ToModel();
            _logger.LogDebug("{0}uccessfully fetched user", model != null ? "S" : "Uns");
            return model;
        }

        public async Task<Models.User> GetUserByEmailAsync(string email, bool fetchClaims = false)
        {
            _logger.LogTrace("Fetch user (by email) {0}", fetchClaims ? "and claims" : "without claims");
            if (fetchClaims)
            {
                return (await _context.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Email == email))?.ToModel();
            }
            var model = (await _context.Users.SingleOrDefaultAsync(x => x.Email == email))?.ToModel();
            _logger.LogDebug("{0}uccessfully fetched user by email", model != null ? "S" : "Uns");
            return model;
        }

        public async Task<Models.User> PatchUserAsync(string subjectId, ILookup<string, string> Properties)
        {
            _logger.LogTrace("Patch user");
            foreach (var values in Properties)
            {
                // todo: ignore protocol claims
                if (values.Key == "email")
                {
                    //todo: special processing, but for now just ignore since we don't allow email changes yet
                }
                else
                {
                    _context.Claims.RemoveRange(_context.Claims.Where(x => x.SubjectId == subjectId && x.Type == values.Key));
                    _context.Claims.AddRange(values.Where(x => x != null).Select(x=> new Entities.UserClaim() { SubjectId = subjectId, Type = values.Key, Value = x }));
                }
            }
            var count = await _context.SaveChangesAsync();
            _logger.LogDebug("Updated {0} claims", count);

            return await GetUserAsync(subjectId);
        }
    }
}
