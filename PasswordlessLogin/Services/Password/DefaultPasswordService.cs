// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Stores;
using StandardResponse;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public class DefaultPasswordService : IPasswordService
    {
        protected readonly ILogger _logger;
        protected readonly IPasswordHashService _passwordHashService;
        protected readonly IPasswordHashStore _passwordHashStore;
        protected readonly PasswordlessLoginOptions _passwordlessLoginOptions;

        public DefaultPasswordService(
            ILogger<DefaultPasswordService> logger,
            IPasswordHashService passwordHashService,
            IPasswordHashStore passwordHashStore,
            PasswordlessLoginOptions passwordlessLoginOptions
            )
        {
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
                status.AddError("Unique identifier is required.");
                return status;
            }
            _logger.LogDebug("Setting password for {0}", uniqueIdentifier);
            if (!PasswordIsStrongEnough(password))
            {
                status.AddError("Password does not meet strength requirements.");
                status.PasswordDoesNotMeetStrengthRequirements = true;
                return status;
            }
            var hash = _passwordHashService.HashPassword(password);
            var removeStatus = await RemovePasswordAsync(uniqueIdentifier);
            if(removeStatus.HasError)
            {
                status.AddError("Failed to update old password.");
                return status;
            }
            var addStatus = await _passwordHashStore.AddPasswordHashAsync(uniqueIdentifier, hash);
            status.Add(addStatus);
            return status;
        }

        public async Task<CheckPasswordStatus> CheckPasswordAsync(string uniqueIdentifier, string password)
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
            if(hashInfo.TempLockUntilUTC > DateTime.UtcNow)
            {
                return CheckPasswordStatus.Error("Password is temporarily locked.", CheckPasswordStatusCode.TemporarilyLocked);
            }
            var checkHashResult = _passwordHashService.CheckPasswordHash(hashInfo.Hash, password);
            switch(checkHashResult)
            {
                case CheckPaswordHashResult.DoesNotMatch:
                    _logger.LogDebug("Password does not match");

                    var currentFailedAttemptCount = hashInfo.FailedAttemptCount + 1;
                    if (currentFailedAttemptCount >= _passwordlessLoginOptions.MaxPasswordFailedAttempts)
                    {                        
                        var lockUntil = DateTime.UtcNow.AddMinutes(_passwordlessLoginOptions.TempLockPasswordMinutes);
                        _logger.LogDebug("Locking password until {0} (UTC)", lockUntil);
                        if(_passwordlessLoginOptions.ResetFailedAttemptCountOnTempLock)
                        {
                            // if not reset, the next failure after a lockout initiates another lockout period
                            currentFailedAttemptCount = 0;
                        }
                        await _passwordHashStore.TempLockPasswordHashAsync(uniqueIdentifier, lockUntil, currentFailedAttemptCount);

                        return CheckPasswordStatus.Error("Password is temporarily locked.", CheckPasswordStatusCode.TemporarilyLocked);
                    }
                    else
                    {
                        _logger.LogDebug("Updating failed attempt count");
                        await _passwordHashStore.UpdatePasswordHashFailureCountAsync(uniqueIdentifier, hashInfo.FailedAttemptCount + 1);
                        return CheckPasswordStatus.Error("Password was incorrect.", CheckPasswordStatusCode.PasswordIncorrect);
                    }
                case CheckPaswordHashResult.MatchesNeedsRehash:
                    _logger.LogDebug("Rehashing password");
                    var newHash = _passwordHashService.HashPassword(password);
                    await _passwordHashStore.UpdatePasswordHashAsync(uniqueIdentifier, newHash);
                    return Status.Success<CheckPasswordStatus>("Password was correct.");
                case CheckPaswordHashResult.Matches:
                    _logger.LogDebug("Password matches");
                    return Status.Success<CheckPasswordStatus>("Password was correct.");
                default:
                    // this should never happen
                    return CheckPasswordStatus.Error("An unexpected error occurred.", CheckPasswordStatusCode.ServiceFailure);
            }
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
            return Response.Success(response.Result.LastChangedUTC, "The date the password was last changed was found.");
        }
    }
}
