// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Extensions;
using SimpleIAM.PasswordlessLogin.Orchestrators;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Services.EventNotification;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.Password;

namespace SimpleIAM.PasswordlessLogin.API
{
    [Route("passwordless-api/v1/my-account")]
    [EnableCors(PasswordlessLoginConstants.Security.CorsPolicyName)]
    [Authorize]
    public class AccountApiController : Controller
    {
        private readonly IEventNotificationService _eventNotificationService;
        private readonly UserOrchestrator _userOrchestrator;
        private readonly IPasswordService _passwordService;
        private readonly IMessageService _messageService;
        private readonly IdProviderConfig _idProviderConfig;
        private readonly IApplicationService _applicationService;

        public AccountApiController(
            IEventNotificationService eventNotificationService,
            UserOrchestrator userOrchestrator,
            IPasswordService passwordService,
            IMessageService messageService,
            IdProviderConfig idProviderConfig,
            IApplicationService applicationService
            )
        {
            _eventNotificationService = eventNotificationService;
            _userOrchestrator = userOrchestrator;
            _passwordService = passwordService;
            _messageService = messageService;
            _idProviderConfig = idProviderConfig;
            _applicationService = applicationService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetUser()
        {
            var subjectId = User.GetSubjectId();
            var response = await _userOrchestrator.GetUserAsync(subjectId);
            if(response.HasError)
            {
                return response.ToJsonResult();
            }
            return PasswordlessLogin.Response.Success(response.Result.ToGetUserViewModel()).ToJsonResult();
        }

        [HttpPatch("")]
        public async Task<IActionResult> PatchUser([FromBody] PatchUserInputModel model)
        {
            var subjectId = User.GetSubjectId();

            var patch = model?.ToPatchUserModel(subjectId);
            var response = await _userOrchestrator.PatchUserAsync(patch);
            if (response.HasError)
            {
                return response.ToJsonResult();
            }
            return PasswordlessLogin.Response.Success(response.Result.ToGetUserViewModel()).ToJsonResult();
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
                if (!ApplicationIdIsNullOrValid(model.ApplicationId))
                {
                    return (new ActionResponse("Invalid application id", HttpStatusCode.BadRequest)).ToJsonResult();
                }

                var subjectId = User.GetSubjectId();
                var getUserResponse = await _userOrchestrator.GetUserAsync(subjectId);
                if(getUserResponse.HasError)
                {
                    return getUserResponse.Status.ToJsonResult();
                }
                var user = getUserResponse.Result;

                if (!string.IsNullOrEmpty(model.OldPassword))
                {
                    var result1 = await _passwordService.CheckPasswordAsync(user, model.OldPassword);
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
                        await _eventNotificationService.NotifyEventAsync(user.Email, EventType.SetPassword);
                        await _messageService.SendPasswordChangedNoticeAsync(model.ApplicationId, user.Email);
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
                if (!ApplicationIdIsNullOrValid(model.ApplicationId))
                {
                    return (new ActionResponse("Invalid application id", HttpStatusCode.BadRequest)).ToJsonResult();
                }

                var subjectId = User.GetSubjectId();
                var getUserResponse = await _userOrchestrator.GetUserAsync(subjectId);
                if (getUserResponse.HasError)
                {
                    return getUserResponse.Status.ToJsonResult();
                }
                var user = getUserResponse.Result;

                if (!string.IsNullOrEmpty(model.OldPassword))
                {
                    var result1 = await _passwordService.CheckPasswordAsync(user, model.OldPassword);
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
                        await _eventNotificationService.NotifyEventAsync(user.Email, EventType.RemovePassword);
                        await _messageService.SendPasswordRemovedNoticeAsync(model.ApplicationId, user.Email);
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
                if (!ApplicationIdIsNullOrValid(model.ApplicationId))
                {
                    return (new ActionResponse("Invalid application id", HttpStatusCode.BadRequest)).ToJsonResult();
                }

                var subjectId = User.GetSubjectId();
                var getUserResponse = await _userOrchestrator.GetUserAsync(subjectId);
                if (getUserResponse.HasError)
                {
                    return getUserResponse.Status.ToJsonResult();
                }
                var user = getUserResponse.Result;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    var result1 = await _passwordService.CheckPasswordAsync(user, model.Password);
                    if (result1 != CheckPasswordResult.Success)
                    {
                        return Unauthenticated("Password was incorrect, locked, or missing");
                    }
                }
                else if (!UserSignedInRecentlyEnoughToChangeSecuritySettings())
                {
                    return Unauthenticated("Please reauthenticate to proceed");
                }

                var response = await _userOrchestrator.ChangeEmailAddressAsync(subjectId, model.NewEmail, model.ApplicationId);
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

        private bool ApplicationIdIsNullOrValid(string applicationId)
        {
            // Duplicate code in AuthenticateOrchestrator
            if (applicationId == null)
            {
                return true;
            }
            if (!_applicationService.ApplicationExists(applicationId))
            {
                //_logger.LogError("Invalid application id '{0}'", applicationId);
                return false;
            }
            return true;
        }
    }
}