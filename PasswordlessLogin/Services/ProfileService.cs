// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Stores;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ILogger _logger;
        private readonly IUserStore _userStore;

        public ProfileService(ILogger<ProfileService> logger, IUserStore userStore)
        {
            _logger = logger;
            _userStore = userStore;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            _logger.LogTrace("Getting profile data");

            var sub = context.Subject.GetSubjectId();
            if(context.RequestedClaimTypes.Any())
            {
                var user = await _userStore.GetUserAsync(sub, true);

                if (user != null)
                {
                    var claims = user.Claims.Select(x => new Claim(x.Type, x.Value)).ToList();
                    claims.Add(new Claim(PasswordlessLoginConstants.StandardClaims.Email, user.Email));
                    claims.Add(new Claim(PasswordlessLoginConstants.StandardClaims.EmailVerified, "true"));
                    if(!claims.Any(x => x.Type == PasswordlessLoginConstants.StandardClaims.Name))
                    {
                        claims.Add(new Claim(PasswordlessLoginConstants.StandardClaims.Name, user.Email));
                    }

                    _logger.LogTrace("Returning requested claims");
                    context.AddRequestedClaims(claims);
                }
            }
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
        }
    }
}
