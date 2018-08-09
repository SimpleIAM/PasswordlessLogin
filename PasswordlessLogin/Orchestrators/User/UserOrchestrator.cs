// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class UserOrchestrator : ActionResponder
    {
        private readonly ILogger _logger;
        private readonly HttpContext _httpContext;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;

        public UserOrchestrator(
            ILogger<UserOrchestrator> logger,
            IHttpContextAccessor httpContextAccessor,
            IUserStore userStore,
            IPasswordService passwordService
            )
        {
            _logger = logger;
            _httpContext = httpContextAccessor.HttpContext;
            _userStore = userStore;
            _passwordService = passwordService;
        }

        public async Task<ActionResponse> GetUserAsync(string subjectId)
        {
            _logger.LogTrace("Get user {0}", subjectId);

            if (!OperatingOnSelf(subjectId))
            {
                _logger.LogWarning("Permission denied trying to get user {0}", subjectId);
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
            _logger.LogTrace("Patch user {0}", model.SubjectId);

            if (!OperatingOnSelf(model.SubjectId))
            {
                _logger.LogWarning("Permission denied trying to patch user {0}", model.SubjectId);
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