// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Entities;
using SimpleIAM.PasswordlessLogin.Models;

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

            // First, if anyone previously used this email, clear the claim that could
            // be used to revert that account back to this email address
            _context.Claims.RemoveRange(_context.Claims.Where(x => 
                x.Type == PasswordlessLoginConstants.Security.PreviousEmailClaimType && 
                x.Value == user.Email));

            var dbUser = new Entities.User()
            {
                SubjectId = user.SubjectId ?? Guid.NewGuid().ToString("N"),
                Email = user.Email,
                CreatedUTC = DateTime.UtcNow
            };
            dbUser.Claims = user.Claims?.Select(x => new Entities.UserClaim() { SubjectId = dbUser.SubjectId, Type = x.Type, Value = x.Value }).ToList();
            await _context.AddAsync(dbUser);
            var count = await _context.SaveChangesAsync();

            var success = count > 0;
            _logger.LogDebug("{0}uccessfully added user", success ? "S" : "Uns");

            user.SubjectId = dbUser.SubjectId;
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

        public async Task<Models.User> GetUserByUsernameAsync(string username, bool fetchClaims = false)
        {
            // NOTE: For this user store username = email. However, other user stores may implement this differently.
            return await GetUserByEmailAsync(username, fetchClaims);
        }

        public async Task<Models.User> GetUserByPreviousEmailAsync(string previousEmail)
        {
            _logger.LogTrace("Fetch user (by previous email) {0}");
            var model = (await _context.Users.SingleOrDefaultAsync(x => x.Claims.Any(c => 
                c.Type == PasswordlessLoginConstants.Security.PreviousEmailClaimType &&
                c.Value == previousEmail
                )))?.ToModel();
            _logger.LogDebug("{0}uccessfully fetched user by previous email", model != null ? "S" : "Uns");
            return model;
        }

        public async Task<Models.User> PatchUserAsync(string subjectId, ILookup<string, string> Properties, bool changeProtectedClaims = false)
        {
            _logger.LogTrace("Patch user");
            Entities.User user;
            foreach (var values in Properties)
            {
                if (changeProtectedClaims && values.Key == "email")
                {
                    var newEmail = values.SingleOrDefault(x => x != null); // will throw if multiple email addresses provided
                    if (newEmail != null)
                    {
                        user = await _context.Users.FirstOrDefaultAsync(x => x.SubjectId == subjectId);
                        user.Email = newEmail;
                    }                    
                }
                else if (PasswordlessLoginConstants.Security.ForbiddenClaims.Contains(values.Key) ||
                    (!changeProtectedClaims && PasswordlessLoginConstants.Security.ProtectedClaims.Contains(values.Key)))
                {
                    // ignore
                    _logger.LogWarning("Attempt to change {0} claim for user {1} was blocked", values.Key, subjectId);
                }
                else
                {
                    _context.Claims.RemoveRange(_context.Claims.Where(x => x.SubjectId == subjectId && x.Type == values.Key));
                    _context.Claims.AddRange(values.Where(x => x != null && x != "").Select(x=> new Entities.UserClaim() { SubjectId = subjectId, Type = values.Key, Value = x }));
                }
            }
            var count = await _context.SaveChangesAsync();
            _logger.LogDebug("Updated {0} claims", count);

            return await GetUserAsync(subjectId);
        }

        public async Task<bool> UserExists(string email)
        {
            _logger.LogTrace("Check if user with username {0} exists", email);

            var user = await GetUserByEmailAsync(email);
            if (user != null)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> UsernameIsAvailable(string email)
        {            
            return !(await UserExists(email));
        }
    }
}
