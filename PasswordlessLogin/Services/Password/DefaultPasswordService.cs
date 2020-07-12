// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Services.Localization;
using SimpleIAM.PasswordlessLogin.Stores;
using StandardResponse;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public class DefaultPasswordService : IPasswordService
    {
        protected readonly IApplicationLocalizer _localizer;
        protected readonly ILogger _logger;
        protected readonly IPasswordHashService _passwordHashService;
        protected readonly IPasswordHashStore _passwordHashStore;
        protected readonly PasswordlessLoginOptions _passwordlessLoginOptions;

        public DefaultPasswordService(
            IApplicationLocalizer localizer,
            ILogger<DefaultPasswordService> logger,
            IPasswordHashService passwordHashService,
            IPasswordHashStore passwordHashStore,
            PasswordlessLoginOptions passwordlessLoginOptions            
            )
        {
            _localizer = localizer;
            _logger = logger;
            _passwordHashService = passwordHashService;
            _passwordHashStore = passwordHashStore;
            _passwordlessLoginOptions = passwordlessLoginOptions;
        }

        public async Task<Status> RemovePasswordAsync(string uniqueIdentifier)
        {
            _logger.LogDebug("Removing password for {0}", uniqueIdentifier);
            return await _passwordHashStore.RemovePasswordHashAsync(uniqueIdentifier);
        }

        public async Task<SetPasswordStatus> SetPasswordAsync(string uniqueIdentifier, string password)
        {
            var status = new SetPasswordStatus();
            if (string.IsNullOrEmpty(uniqueIdentifier))
            {
                status.AddError(_localizer["Unique identifier is required."]);
                return status;
            }
            _logger.LogDebug("Setting password for {0}", uniqueIdentifier);
            if (!PasswordIsStrongEnough(password))
            {
                status.AddError(_localizer["Password does not meet minimum strength requirements."]);
                status.PasswordDoesNotMeetStrengthRequirements = true;
                return status;
            }
            var hash = _passwordHashService.HashPassword(password);
            var removeStatus = await RemovePasswordAsync(uniqueIdentifier);
            if(removeStatus.HasError)
            {
                status.AddError(_localizer["Failed to update old password."]);
                return status;
            }
            var addStatus = await _passwordHashStore.AddPasswordHashAsync(uniqueIdentifier, hash);
            status.Add(addStatus);
            return status;
        }

        public async Task<CheckPasswordStatus> CheckPasswordAsync(string uniqueIdentifier, string password, PasswordLockMode lockMode)
        {
            _logger.LogDebug("Checking password for {0}", uniqueIdentifier);
            var response = await _passwordHashStore.GetPasswordHashAsync(uniqueIdentifier);
            if(response.HasError)
            {
                var status = new CheckPasswordStatus();
                status.Add(response.Status);
                status.StatusCode = CheckPasswordStatusCode.NotFound;
                return status;
            }
            var hashInfo = response.Result;


            if (AccountIsLocked(lockMode, hashInfo.FailedAttemptCount, hashInfo.TempLockUntilUTC))
            {
                return CheckPasswordStatus.Error(_localizer["Password is temporarily locked."], CheckPasswordStatusCode.TemporarilyLocked);
            }

            var checkHashResult = _passwordHashService.CheckPasswordHash(hashInfo.Hash, password);
            switch(checkHashResult)
            {
                case CheckPasswordHashResult.DoesNotMatch:
                    return await ProcessDoesNotMatchAndReturnAsync(uniqueIdentifier, lockMode, hashInfo.FailedAttemptCount);
                case CheckPasswordHashResult.MatchesNeedsRehash:
                    return await ProcessMatchesNeedsRehashAndReturnAsync(uniqueIdentifier, password);
                case CheckPasswordHashResult.Matches:
                    return await ProcessMatchesAndReturnAsync(uniqueIdentifier);
                default:
                    // this should never happen
                    return CheckPasswordStatus.Error(_localizer["An unexpected error occurred."], CheckPasswordStatusCode.ServiceFailure);
            }
        }

        protected bool AccountIsLocked(PasswordLockMode lockMode, int failedAttemptCount, DateTime? tempLockUntilUTC)
        {
            var canLock = lockMode != PasswordLockMode.DoNotLock;
            var lockoutUntrustedClient =
                lockMode == PasswordLockMode.UntrustedClient
                && failedAttemptCount >= _passwordlessLoginOptions.MaxUntrustedPasswordFailedAttempts;
            var tempLockout = tempLockUntilUTC > DateTime.UtcNow;

            return canLock && (lockoutUntrustedClient || tempLockout);
        }

        protected async Task<CheckPasswordStatus> ProcessDoesNotMatchAndReturnAsync(string uniqueIdentifier, PasswordLockMode lockMode, int failedAttemptCount)
        {
            _logger.LogDebug("Password does not match.");
            if(lockMode == PasswordLockMode.DoNotLock)
            {
                return CheckPasswordStatus.Error(_localizer["Password was not correct."], CheckPasswordStatusCode.PasswordIncorrect);
            }

            var currentFailedAttemptCount = failedAttemptCount + 1;
            if (currentFailedAttemptCount >= _passwordlessLoginOptions.TempLockPasswordFailedAttemptCount)
            {
                var lockUntil = DateTime.UtcNow.AddMinutes(_passwordlessLoginOptions.TempLockPasswordMinutes);
                _logger.LogDebug("Locking password until {0} (UTC)", lockUntil);
                if (lockMode == PasswordLockMode.TrustedClient)
                {
                    // for sign in attempts from a trusted client, we reset the failure account when doing a temp
                    // lock so that the next failure after a lockout will not initiate another lockout period
                    currentFailedAttemptCount = 0;
                }
                await _passwordHashStore.UpdatePasswordHashTempLockAsync(uniqueIdentifier, lockUntil, currentFailedAttemptCount);

                return CheckPasswordStatus.Error(_localizer["Password is temporarily locked."], CheckPasswordStatusCode.TemporarilyLocked);
            }
            else
            {
                _logger.LogDebug("Updating failed attempt count");
                await _passwordHashStore.UpdatePasswordHashFailureCountAsync(uniqueIdentifier, failedAttemptCount + 1);
                return CheckPasswordStatus.Error(_localizer["Password was not correct."], CheckPasswordStatusCode.PasswordIncorrect);
            }
        }

        protected async Task<CheckPasswordStatus> ProcessMatchesNeedsRehashAndReturnAsync(string uniqueIdentifier, string password)
        {
            _logger.LogDebug("Rehashing password");
            var newHash = _passwordHashService.HashPassword(password);
            var updateStatus = await _passwordHashStore.UpdatePasswordHashAsync(uniqueIdentifier, newHash);
            if (updateStatus.HasError)
            {
                _logger.LogWarning("Password should be rehashed, but unable to update the password hash for user {0}.", uniqueIdentifier);
            }
            return Status.Success<CheckPasswordStatus>(_localizer["Password was correct."]);
        }

        protected async Task<CheckPasswordStatus> ProcessMatchesAndReturnAsync(string uniqueIdentifier)
        {
            _logger.LogDebug("Password matches");
            var updateStatus = await _passwordHashStore.UpdatePasswordHashTempLockAsync(uniqueIdentifier, null, 0);
            if (updateStatus.HasError)
            {
                _logger.LogWarning("Could not clear failure count for user {0}.", uniqueIdentifier);
            }
            return Status.Success<CheckPasswordStatus>(_localizer["Password was correct."]);
        }

        private bool PasswordIsStrongEnough(string password)
        {
            return password?.Length >= _passwordlessLoginOptions.MinimumPasswordLength;
        }

        public async Task<Response<DateTime, Status>> PasswordLastChangedAsync(string uniqueIdentifier)
        {
            var response = await _passwordHashStore.GetPasswordHashAsync(uniqueIdentifier);
            if(response.HasError)
            {
                return new Response<DateTime, Status>(response.Status);
            }
            return Response.Success(response.Result.LastChangedUTC, _localizer["The date the password was last changed was found."]);
        }
    }
}
