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
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class UserOrchestrator : ActionResponder
    {
        private readonly ILogger _logger;
        private readonly IEventNotificationService _eventNotificationService;
        private readonly HttpContext _httpContext;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly IdProviderConfig _idProviderConfig;

        public UserOrchestrator(
            ILogger<UserOrchestrator> logger,
            IEventNotificationService eventNotificationService,
            IHttpContextAccessor httpContextAccessor,
            IUserStore userStore,
            IPasswordService passwordService,
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            IdProviderConfig idProviderConfig)
        {
            _logger = logger;
            _eventNotificationService = eventNotificationService;
            _httpContext = httpContextAccessor.HttpContext;
            _userStore = userStore;
            _passwordService = passwordService;
            _oneTimeCodeService = oneTimeCodeService;
            _messageService = messageService;
            _idProviderConfig = idProviderConfig;
        }

        public async Task<Response<User>> GetUserAsync(string subjectId)
        {
            _logger.LogTrace("Get user {0}", subjectId);

            if (!OperatingOnSelf(subjectId))
            {
                _logger.LogWarning("Permission denied trying to get user {0}", subjectId);
                return Response.Error<User>("Permission denied.", HttpStatusCode.Forbidden);
            }
            var user = await _userStore.GetUserAsync(subjectId, true);
            if(user != null)
            {
                return Response.Success(user);
            }
            return Response.Error<User>("Not found.", HttpStatusCode.NotFound);
        }

        public async Task<Response<User>> PatchUserAsync(PatchUserModel model)
        {
            _logger.LogTrace("Patch user {0}", model.SubjectId);

            if (!OperatingOnSelf(model.SubjectId))
            {
                _logger.LogWarning("Permission denied trying to patch user {0}", model.SubjectId);
                return Response.Error<User>("Permission denied.", HttpStatusCode.Forbidden);
            }
            var user = await _userStore.GetUserAsync(model.SubjectId, true);
            if (user == null)
            {
                return Response.Error<User>("User does not exist", HttpStatusCode.BadRequest);
            }
            var updatedUser = await _userStore.PatchUserAsync(model.SubjectId, model.Properties);
            await _eventNotificationService.NotifyEventAsync(updatedUser.Email, EventType.UpdateAccount);
            return Response.Success(updatedUser);
        }

        public async Task<Response<ChangeEmailViewModel>> ChangeEmailAddressAsync(string subjectId, string newEmail, string applicationId = null)
        {
            _logger.LogTrace("Change email for user {0} to {1}", subjectId, newEmail);

            if (!OperatingOnSelf(subjectId))
            {
                _logger.LogWarning("Permission denied trying to change email address user {0}", subjectId);
                return Response.Error<ChangeEmailViewModel>("Permission denied.", HttpStatusCode.Forbidden);
            }
            var user = await _userStore.GetUserAsync(subjectId, true);
            if (user == null)
            {
                return Response.Error<ChangeEmailViewModel>("User does not exist", HttpStatusCode.BadRequest);
            }
            // check if a cancel email change code link is still valid
            var previouslyChangedEmail = user.Claims.FirstOrDefault(x => x.Type == PasswordlessLoginConstants.Security.PreviousEmailClaimType)?.Value;
            if(previouslyChangedEmail != null && (await _oneTimeCodeService.UnexpiredOneTimeCodeExistsAsync(previouslyChangedEmail)))
            {
                return Response.Error<ChangeEmailViewModel>("You cannot change your email address again until the link to cancel the last email change (sent to your old email address) expires.", HttpStatusCode.Forbidden);
            }
            if (!(await UsernameIsReallyAvailableAsync(newEmail)))
            {
                // todo: review potential leak of who has existing accounts
                return Response.Error<ChangeEmailViewModel>("Username is not available", HttpStatusCode.Forbidden);
            }
            var oldEmail = user.Email;
            
            var otc = await _oneTimeCodeService.GetOneTimeCodeAsync(oldEmail, TimeSpan.FromHours(_idProviderConfig.CancelEmailChangeTimeWindowHours));
            var result = await _messageService.SendEmailChangedNoticeAsync(applicationId, oldEmail, otc.LongCode);
            if(result.MessageSent)
            {
                var changes = new Dictionary<string, string>
                {
                    ["email"] = newEmail,
                    [PasswordlessLoginConstants.Security.PreviousEmailClaimType] = oldEmail
                };
                var updatedUser = await _userStore.PatchUserAsync(subjectId, changes.ToLookup(x => x.Key, x => x.Value), true);
                var viewModel = new ChangeEmailViewModel()
                {
                    OldEmail = user.Email,
                    NewEmail = updatedUser.Email,
                };
                await _eventNotificationService.NotifyEventAsync(viewModel.OldEmail, EventType.EmailChange, $"Changed to {viewModel.NewEmail}");
                return Response.Success(viewModel);
            }
            else
            {
                return Response.Error<ChangeEmailViewModel>($"Change cancelled because of failure to send email notice: {result.ErrorMessageForEndUser}");
            }
        }

        public async Task<Response<ChangeEmailViewModel>> CancelEmailAddressChangeAsync(string longCode)
        {
            _logger.LogTrace("Cancel email address change");

            var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(longCode, null);
            if(response.Result != CheckOneTimeCodeResult.VerifiedWithoutNonce)
            {
                return Response.Error<ChangeEmailViewModel>("Invalid code.", HttpStatusCode.BadRequest);
            }
            var user = await _userStore.GetUserByPreviousEmailAsync(response.SentTo);
            if (user == null)
            {
                return Response.Error<ChangeEmailViewModel>("User not found.", HttpStatusCode.BadRequest);
            }
            var changes = new Dictionary<string, string>
            {
                ["email"] = response.SentTo,
                [PasswordlessLoginConstants.Security.PreviousEmailClaimType] = null
            };
            var updatedUser = await _userStore.PatchUserAsync(user.SubjectId, changes.ToLookup(x => x.Key, x => x.Value), true);
            var viewModel = new ChangeEmailViewModel()
            {
                OldEmail = user.Email,
                NewEmail = updatedUser.Email,
            };
            await _eventNotificationService.NotifyEventAsync(viewModel.OldEmail, EventType.CancelEmailChange, $"Reverted to {viewModel.NewEmail}");
            return Response.Success(viewModel);
        }

        public async Task<bool> UsernameIsReallyAvailableAsync(string email)
        {
            // Note: a username that has been recently "released" via a change of email address will
            // not become available until the one time link that can cancel the change expires.

            return (await _userStore.UsernameIsAvailable(email))
                && !(await _oneTimeCodeService.UnexpiredOneTimeCodeExistsAsync(email));
        }

        private bool OperatingOnSelf(string subjectId)
        {
            return subjectId == _httpContext.User.GetSubjectId();
        }
    }
}