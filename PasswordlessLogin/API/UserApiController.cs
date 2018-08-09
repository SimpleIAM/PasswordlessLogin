// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Orchestrators;

namespace SimpleIAM.PasswordlessLogin.API
{
    [Route("api/v1/user")]
    [EnableCors(PasswordlessLoginConstants.Security.CorsPolicyName)]
    [Authorize]
    public class UserApiController : Controller
    {
        private readonly UserOrchestrator _userOrchestrator;

        public UserApiController(UserOrchestrator userOrchestrator)
        {
            _userOrchestrator = userOrchestrator;            
        }

        [HttpGet("{subjectId}")]
        public async Task<IActionResult> GetUser(string subjectId)
        {
            var response = await _userOrchestrator.GetUserAsync(subjectId);
            if(response.Content is Models.User)
            {
                response.Content = (response.Content as Models.User)?.ToGetUserViewModel();
            }
            return response.ToJsonResult();
        }

        [HttpPatch("{subjectId}")]
        public async Task<IActionResult> PatchUser(string subjectId, [FromBody] PatchUserInputModel model)
        {
            var patch = model?.ToPatchUserModel(subjectId);
            var response = await _userOrchestrator.PatchUserAsync(patch);
            if (response.Content is Models.User)
            {
                response.Content = (response.Content as Models.User)?.ToGetUserViewModel();
            }
            return response.ToJsonResult();
        }
    }
}