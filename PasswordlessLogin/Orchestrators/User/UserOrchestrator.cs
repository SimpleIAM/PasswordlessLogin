// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Services.EventNotification;
using SimpleIAM.PasswordlessLogin.Services.Localization;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class UserOrchestrator
    {
        private readonly ILogger _logger;
        private readonly IEventNotificationService _eventNotificationService;
        private readonly HttpContext _httpContext;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly PasswordlessLoginOptions _passwordlessLoginOptions;
        private readonly IApplicationLocalizer _localizer;

        public UserOrchestrator(
            ILogger<UserOrchestrator> logger,
            IEventNotificationService eventNotificationService,
            IHttpContextAccessor httpContextAccessor,
            IUserStore userStore,
            IPasswordService passwordService,
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            PasswordlessLoginOptions passwordlessLoginOptions,
            IApplicationLocalizer localizer)
        {
            _logger = logger;
            _eventNotificationService = eventNotificationService;
            _httpContext = httpContextAccessor.HttpContext;
            _userStore = userStore;
            _passwordService = passwordService;
            _oneTimeCodeService = oneTimeCodeService;
            _messageService = messageService;
            _passwordlessLoginOptions = passwordlessLoginOptions;
            _localizer = localizer;
        }

        public async Task<Response<User, WebStatus>> GetUserAsync()
        {
            var subjectId = _httpContext.User.GetSubjectId();
            _logger.LogTrace("Get user {0}", subjectId);

            var userResponse = await _userStore.GetUserAsync(subjectId, true);
            if(userResponse.HasError)
            {
                return Response.Web.Error<User>(_localizer["User not found."], HttpStatusCode.NotFound);
            }
            return Response.Web.Success(userResponse.Result);
        }

        public async Task<Response<User, WebStatus>> PatchUserAsync(PatchUserModel model)
        {
            _logger.LogTrace("Patch user {0}", _httpContext.User.GetSubjectId());

            var userResponse = await GetUserAsync();
            if (userResponse.HasError)
            {
                return userResponse;
            }
            var updateUserResponse = await _userStore.PatchUserAsync(userResponse.Result.SubjectId, model.Properties);
            if(updateUserResponse.HasError)
            {
                var status = new WebStatus(updateUserResponse.Status);
                status.StatusCode = HttpStatusCode.BadRequest;
                return new Response<User, WebStatus>(status);
            }
            var updatedUser = updateUserResponse.Result;
            await _eventNotificationService.NotifyEventAsync(updatedUser.Email, EventType.UpdateAccount);
            return Response.Web.Success(updatedUser);
        }

        public async Task<Response<ChangeEmailViewModel, WebStatus>> ChangeEmailAddressAsync(
            string newEmail, string applicationId = null)
        {
            _logger.LogTrace("Change email for user {0} to {1}", _httpContext.User.GetSubjectId(), newEmail);

            var userResponse = await GetUserAsync();
            if (userResponse.HasError)
            {
                return Response.Web.Error<ChangeEmailViewModel>("User not found.", HttpStatusCode.NotFound);
            }
            var user = userResponse.Result;

            // Check if a cancel email change code link is still valid. Note that the cancel email change code link does
            // not interfere with getting a one time code to sign in because it is associated with the OLD email address.
            var previouslyChangedEmail = user.Claims.FirstOrDefault(x => x.Type == PasswordlessLoginConstants.Security.PreviousEmailClaimType)?.Value;
            if(previouslyChangedEmail != null && (await _oneTimeCodeService.UnexpiredOneTimeCodeExistsAsync(previouslyChangedEmail)))
            {
                return Response.Web.Error<ChangeEmailViewModel>(_localizer["You cannot change your email address again until the link to cancel the last email change (sent to your old email address) expires."], HttpStatusCode.Forbidden);
            }
            var usernameAvailableStatus = await UsernameIsReallyAvailableAsync(newEmail, user.SubjectId);
            if (usernameAvailableStatus.HasError)
            {
                return Response.Web.Error<ChangeEmailViewModel>(_localizer["Username is not available."], HttpStatusCode.Conflict);
            }
            var oldEmail = user.Email;

            if (_passwordlessLoginOptions.SendCancelEmailChangeMessage)
            {
                var otcResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(oldEmail, TimeSpan.FromHours(_passwordlessLoginOptions.CancelEmailChangeTimeWindowHours));
                var status = await _messageService.SendEmailChangedNoticeAsync(applicationId, oldEmail, otcResponse.Result.LongCode);
                if (status.HasError)
                {
                    // TODO: review to ensure that this will not prevent changing one's email address if the
                    // old address is undeliverable
                    return Response.Web.Error<ChangeEmailViewModel>($"{_localizer["Change cancelled because of failure to send email notice:"]} {status.Text}");
                }
            }

            var changes = new Dictionary<string, string>
            {
                ["email"] = newEmail,
                [PasswordlessLoginConstants.Security.PreviousEmailClaimType] = oldEmail
            };
            var updateUserResponse = await _userStore.PatchUserAsync(user.SubjectId, changes.ToLookup(x => x.Key, x => x.Value), true);
            if(updateUserResponse.HasError)
            {
                var patchStatus = new WebStatus(updateUserResponse.Status);
                patchStatus.StatusCode = HttpStatusCode.BadRequest;
                return new Response<ChangeEmailViewModel, WebStatus>(patchStatus);
            }
            var updatedUser = updateUserResponse.Result;
            var viewModel = new ChangeEmailViewModel()
            {
                OldEmail = user.Email,
                NewEmail = updatedUser.Email,
            };
            await _eventNotificationService.NotifyEventAsync(viewModel.OldEmail, EventType.EmailChange, $"Changed to {viewModel.NewEmail}");
            return Response.Web.Success(viewModel);
        }

        public async Task<Response<ChangeEmailViewModel, WebStatus>> CancelEmailAddressChangeAsync(string longCode)
        {
            _logger.LogTrace("Cancel email address change");

            var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(longCode, null);
            if(response.Status.StatusCode != CheckOneTimeCodeStatusCode.VerifiedWithoutNonce) // TODO: investigate if VerifiedWithNonce is possible and valid
            {
                return Response.Web.Error<ChangeEmailViewModel>(_localizer["Invalid code."], HttpStatusCode.BadRequest);
            }
            var userResponse = await _userStore.GetUserByPreviousEmailAsync(response.Result.SentTo);
            if (userResponse.HasError)
            {
                return Response.Web.Error<ChangeEmailViewModel>(_localizer["User not found."], HttpStatusCode.BadRequest);
            }
            var user = userResponse.Result;

            var changes = new Dictionary<string, string>
            {
                ["email"] = response.Result.SentTo,
                [PasswordlessLoginConstants.Security.PreviousEmailClaimType] = null
            };
            var updateUserResponse = await _userStore.PatchUserAsync(user.SubjectId, changes.ToLookup(x => x.Key, x => x.Value), true);
            if(updateUserResponse.HasError)
            {
                var patchStatus = new WebStatus(updateUserResponse.Status);
                patchStatus.StatusCode = HttpStatusCode.BadRequest;
                return new Response<ChangeEmailViewModel, WebStatus>(patchStatus);
            }
            var updatedUser = updateUserResponse.Result;
            var viewModel = new ChangeEmailViewModel()
            {
                OldEmail = user.Email,
                NewEmail = updatedUser.Email,
            };
            await _eventNotificationService.NotifyEventAsync(viewModel.OldEmail, EventType.CancelEmailChange, $"Reverted to {viewModel.NewEmail}");
            return Response.Web.Success(viewModel, _localizer["Email address change has been reverted."]);
        }

        public async Task<Status> UsernameIsReallyAvailableAsync(string email, string subjectId)
        {
            // Note: a username that has been recently "released" via a change of email address will
            // not become available until the one time link that can cancel the change expires.

            var available = (await _userStore.UsernameIsAvailable(email, subjectId))
                && !(await _oneTimeCodeService.UnexpiredOneTimeCodeExistsAsync(email));

            return available 
                ? Status.Success(_localizer["Username is available."]) 
                : Status.Error(_localizer["Username is not available."]);
        }
    }
}