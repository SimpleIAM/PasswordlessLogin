// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;

namespace SimpleIAM.OpenIdAuthority.Orchestrators
{
    public class UserOrchestrator : ActionResponder
    {
        private readonly HttpContext _httpContext;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;

        public UserOrchestrator(
            IHttpContextAccessor httpContextAccessor,
            IUserStore userStore,
            IPasswordService passwordService
            )
        {
            _httpContext = httpContextAccessor.HttpContext;
            _userStore = userStore;
            _passwordService = passwordService;
        }

        public async Task<ActionResponse> GetUserAsync(string subjectId)
        {
            if(!OperatingOnSelf(subjectId))
            {
                return PermissionDenied();
            }
            var user = await _userStore.GetUserAsync(subjectId, true);
            if(user != null)
            {
                return new ActionResponse(user);
            }
            return NotFound();
        }

        public async Task<ActionResponse> PatchUserAsync(PatchUserModel model)
        {
            if (!OperatingOnSelf(model.SubjectId))
            {
                return PermissionDenied();
            }
            var user = await _userStore.GetUserAsync(model.SubjectId, true);
            if (user == null)
            {
                return BadRequest("User does not exist");
            }
            var updatedUser = await _userStore.PatchUserAsync(model.SubjectId, model.Properties);
            return new ActionResponse(updatedUser);
        }

        private bool OperatingOnSelf(string subjectId)
        {
            return subjectId == _httpContext.User.GetSubjectId();
        }
    }
}