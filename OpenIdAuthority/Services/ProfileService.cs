// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using SimpleIAM.OpenIdAuthority.Stores;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUserStore _userStore;

        public ProfileService(IUserStore userStore)
        {
            _userStore = userStore;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();
            if(context.RequestedClaimTypes.Any())
            {
                var user = await _userStore.GetUserAsync(sub, true);

                if (user != null)
                {
                    var claims = user.Claims.Select(x => new Claim(x.Type, x.Value));
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
