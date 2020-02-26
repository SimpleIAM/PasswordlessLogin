// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Entities;
using SimpleIAM.PasswordlessLogin.Helpers;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Services.Localization;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public class DbUserStore : IUserStore
    {
        private readonly IApplicationLocalizer _localizer;
        protected readonly ILogger _logger;
        protected readonly PasswordlessLoginDbContext _context;

        public DbUserStore(IApplicationLocalizer localizer, ILogger<DbUserStore> logger, PasswordlessLoginDbContext context)
        {
            _localizer = localizer;
            _logger = logger;
            _context = context;
        }

        public async Task<Response<Models.User, Status>> AddUserAsync(Models.User user)
        {
            user.Email = user.Email?.ToLowerInvariant();

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
            if (count == 0)
            {
                return Response.Error<Models.User>(_localizer["Failed to add the user."]);
            }

            user.SubjectId = dbUser.SubjectId;
            return Response.Success(user, _localizer["User saved."]);
        }

        public async Task<Response<Models.User, Status>> GetUserAsync(string subjectId, bool fetchClaims = false)
        {
            _logger.LogTrace("Fetch user {0}", fetchClaims ? "and claims" : "without claims");
            Models.User user;
            if (fetchClaims)
            {
                user = (await _context.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.SubjectId == subjectId))?.ToModel();
            }
            user = (await _context.Users.FindAsync(subjectId))?.ToModel();
            if (user == null)
            {
                return Response.Error<Models.User>(_localizer["User not found."]);
            }

            return Response.Success(user, _localizer["User found."]);
        }

        public async Task<Response<Models.User, Status>> GetUserByEmailAsync(string email, bool fetchClaims = false)
        {
            email = email?.ToLowerInvariant();

            _logger.LogTrace("Fetch user (by email) {0}", fetchClaims ? "and claims" : "without claims");
            Models.User user;
            if (fetchClaims)
            {
                user = (await _context.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Email == email))?.ToModel();
            }
            user = (await _context.Users.SingleOrDefaultAsync(x => x.Email == email))?.ToModel();
            if (user == null)
            {
                return Response.Error<Models.User>(_localizer["User not found."]);
            }

            return Response.Success(user, _localizer["User found."]);
        }

        public async Task<Response<Models.User, Status>> GetUserByUsernameAsync(string username, bool fetchClaims = false)
        {
            // NOTE: For this user store username = email. However, other user stores may implement this differently.
            return await GetUserByEmailAsync(username, fetchClaims);
        }

        public async Task<Response<Models.User, Status>> GetUserByPreviousEmailAsync(string previousEmail)
        {
            previousEmail = previousEmail?.ToLowerInvariant();

            _logger.LogTrace("Fetch user (by previous email) {0}", previousEmail);
            var user = (await _context.Users.SingleOrDefaultAsync(x => x.Claims.Any(c => 
                c.Type == PasswordlessLoginConstants.Security.PreviousEmailClaimType &&
                c.Value == previousEmail
                )))?.ToModel();

            if (user == null)
            {
                return Response.Error<Models.User>(_localizer["User not found."]);
            }

            return Response.Success(user, _localizer["User found."]);
        }

        public async Task<Response<Models.User, Status>> PatchUserAsync(string subjectId, ILookup<string, string> Properties, bool changeProtectedClaims = false)
        {
            var status = new Status();
            _logger.LogTrace("Patch user");
            Entities.User user;
            foreach (var values in Properties)
            {
                if (changeProtectedClaims && values.Key == "email")
                {
                    var emails = values.Where(email => email != null).ToList();
                    if(emails.Count > 1)
                    {
                        return Response.Error<Models.User>(_localizer["Only one email address is allowed."]);
                    }
                    if (emails.Count == 1)
                    {
                        var newEmail = emails.First()?.ToLowerInvariant();
                        user = await _context.Users.FirstOrDefaultAsync(x => x.SubjectId == subjectId);
                        user.Email = newEmail;
                    }
                }
                else if (PasswordlessLoginConstants.Security.ForbiddenClaims.Contains(values.Key) ||
                    (!changeProtectedClaims && PasswordlessLoginConstants.Security.ProtectedClaims.Contains(values.Key)))
                {
                    // ignore
                    _logger.LogWarning("Attempt to change {0} claim for user {1} was blocked", values.Key, subjectId);
                    status.AddWarning(_localizer["Changing '{0}' is not allowed.", values.Key]);
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

        public async Task<bool> UserExists(string username)
        {
            _logger.LogTrace("Check if user with username {0} exists", username);

            var response = await GetUserByUsernameAsync(username);
            return response.IsOk;
        }

        public async Task<bool> UsernameIsAvailable(string username)
        {
            // In this user store, usernames are restricted to email address until we implement cell phone usernames
            return !(await UserExists(username)) && EmailAddressChecker.EmailIsValid(username); 
        }
    }
}
