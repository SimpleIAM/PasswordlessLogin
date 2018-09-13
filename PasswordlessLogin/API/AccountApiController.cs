// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Orchestrators;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.Password;

namespace SimpleIAM.PasswordlessLogin.API
{
    [Route("passwordless-api/v1/my-account")]
    [EnableCors(PasswordlessLoginConstants.Security.CorsPolicyName)]
    [Authorize]
    public class AccountApiController : Controller
    {
        private readonly UserOrchestrator _userOrchestrator;
        private readonly IPasswordService _passwordService;
        private readonly IMessageService _messageService;
        private readonly IdProviderConfig _idProviderConfig;

        public AccountApiController(
            UserOrchestrator userOrchestrator,
            IPasswordService passwordService,
            IMessageService messageService,
            IdProviderConfig idProviderConfig
            )
        {
            _userOrchestrator = userOrchestrator;
            _passwordService = passwordService;
            _messageService = messageService;
            _idProviderConfig = idProviderConfig;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetUser()
        {
            var subjectId = User.GetSubjectId();
            var response = await _userOrchestrator.GetUserAsync(subjectId);
            if(response.Content is Models.User)
            {
                response.Content = (response.Content as Models.User)?.ToGetUserViewModel();
            }
            return response.ToJsonResult();
        }

        [HttpPatch("")]
        public async Task<IActionResult> PatchUser([FromBody] PatchUserInputModel model)
        {
            var subjectId = User.GetSubjectId();

            var patch = model?.ToPatchUserModel(subjectId);
            var response = await _userOrchestrator.PatchUserAsync(patch);
            if (response.Content is Models.User)
            {
                response.Content = (response.Content as Models.User)?.ToGetUserViewModel();
            }
            return response.ToJsonResult();
        }

        [HttpGet("date-password-set")]
        public async Task<IActionResult> DatePasswordSet()
        {
            var subjectId = User.GetSubjectId();
            var date = await _passwordService.PasswordLastChangedAsync(subjectId);
            return Json(new { date });
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordInputModel model)
        {
            if (ModelState.IsValid)
            {
                var subjectId = User.GetSubjectId();

                if (!string.IsNullOrEmpty(model.OldPassword))
                {
                    var result1 = await _passwordService.CheckPasswordAsync(subjectId, model.OldPassword);
                    if (result1 != CheckPasswordResult.Success)
                    {
                        return Unauthenticated("Old password was incorrect, locked, or missing");
                    }
                }
                else if (!UserSignedInRecentlyEnoughToChangeSecuritySettings())
                {
                    return Unauthenticated("Please reauthenticate to proceed");
                }

                var result = await _passwordService.SetPasswordAsync(subjectId, model.NewPassword);
                switch (result)
                {
                    case SetPasswordResult.Success:
                        var response = await _userOrchestrator.GetUserAsync(subjectId);
                        if (response.Content is Models.User)
                        {
                            var user = response.Content as Models.User;
                            await _messageService.SendPasswordChangedNoticeAsync(user.Email);
                        }
                        return Ok();
                    case SetPasswordResult.PasswordDoesNotMeetStrengthRequirements:
                        ModelState.AddModelError("NewPassword", "Password does not meet minimum password strength requirements (try something longer).");
                        break;
                    case SetPasswordResult.ServiceFailure:
                        ModelState.AddModelError("", "Something went wrong.");
                        break;
                }
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        [HttpPost("remove-password")]
        public async Task<IActionResult> RemovePassword([FromBody] RemovePasswordInputModel model)
        {
            if (ModelState.IsValid)
            {
                var subjectId = User.GetSubjectId();

                if (!string.IsNullOrEmpty(model.OldPassword))
                {
                    var result1 = await _passwordService.CheckPasswordAsync(subjectId, model.OldPassword);
                    if (result1 != CheckPasswordResult.Success)
                    {
                        return Unauthenticated("Old password was incorrect, locked, or missing");
                    }
                }
                else if (!UserSignedInRecentlyEnoughToChangeSecuritySettings())
                {
                    return Unauthenticated("Please reauthenticate to proceed");
                }

                var result = await _passwordService.RemovePasswordAsync(subjectId);
                switch (result)
                {
                    case RemovePasswordResult.Success:
                        var response = await _userOrchestrator.GetUserAsync(subjectId);
                        if (response.Content is Models.User)
                        {
                            var user = response.Content as Models.User;
                            await _messageService.SendPasswordRemovedNoticeAsync(user.Email);
                        }
                        return Ok();
                    case RemovePasswordResult.ServiceFailure:
                        ModelState.AddModelError("", "Something went wrong.");
                        break;
                }
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailInputModel model)
        {
            if (ModelState.IsValid)
            {
                var subjectId = User.GetSubjectId();

                if (!string.IsNullOrEmpty(model.Password))
                {
                    var result1 = await _passwordService.CheckPasswordAsync(subjectId, model.Password);
                    if (result1 != CheckPasswordResult.Success)
                    {
                        return Unauthenticated("Password was incorrect, locked, or missing");
                    }
                }
                else if (!UserSignedInRecentlyEnoughToChangeSecuritySettings())
                {
                    return Unauthenticated("Please reauthenticate to proceed");
                }

                var response = await _userOrchestrator.ChangeEmailAddressAsync(subjectId, model.NewEmail);
                return response.ToJsonResult();
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        private bool UserSignedInRecentlyEnoughToChangeSecuritySettings()
        {
            // Must be less than X minutes since the user signed in
            return User.GetAuthTimeUTC().AddMinutes(_idProviderConfig.ChangeSecuritySettingsTimeWindowMinutes) > DateTime.UtcNow;
        }

        private JsonResult Ok(string message = null)
        {
            return (new ActionResponse(message)).ToJsonResult();
        }

        private JsonResult Unauthenticated(string message = null)
        {
            return (new ActionResponse(message, HttpStatusCode.Unauthorized)).ToJsonResult();
        }
    }
}