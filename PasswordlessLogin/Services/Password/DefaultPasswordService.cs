// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Stores;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public class DefaultPasswordService : IPasswordService
    {
        private readonly ILogger _logger;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IPasswordHashStore _passwordHashStore;
        private readonly IdProviderConfig _idProviderConfig;

        public DefaultPasswordService(
            ILogger<DefaultPasswordService> logger,
            IPasswordHashService passwordHashService,
            IPasswordHashStore passwordHashStore,
            IdProviderConfig idProviderConfig
            )
        {
            _logger = logger;
            _passwordHashService = passwordHashService;
            _passwordHashStore = passwordHashStore;
            _idProviderConfig = idProviderConfig;
        }

        public string UniqueIdentifierClaimType => "sub";

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
                status.AddError("Unique identifier is required.", HttpStatusCode.BadRequest);
                return status;
            }
            _logger.LogDebug("Setting password for {0}", uniqueIdentifier);
            if (!PasswordIsStrongEnough(password))
            {
                status.AddError("Password does not meet strength requirements.", HttpStatusCode.BadRequest);
                status.PasswordDoesNotMeetStrengthRequirements = true;
                return status;
            }
            var hash = _passwordHashService.HashPassword(password);
            var removeStatus = await RemovePasswordAsync(uniqueIdentifier);
            if(status.HasError)
            {
                status.AddError("Failed to update old password.");
                return status;
            }
            var addStatus = await _passwordHashStore.AddPasswordHashAsync(uniqueIdentifier, hash);
            status.Add(addStatus);
            return status;
        }

        public async Task<CheckPasswordStatus> CheckPasswordAsync(User user, string password)
        {
            _logger.LogDebug("Checking password for {0}", user.SubjectId);
            var status = new CheckPasswordStatus();
            var response = await _passwordHashStore.GetPasswordHashAsync(user.SubjectId);
            if(response.HasError)
            {
                status.Add(response.Status);
                status.StatusCode = HttpStatusCode.Unauthorized;
                return status;
            }
            var hashInfo = response.Result;
            if(hashInfo.TempLockUntilUTC > DateTime.UtcNow)
            {
                status.AddError("Password is temporarily locked.", HttpStatusCode.Unauthorized);
                status.TemporarilyLocked = true;
                return status;
            }
            var checkHashResult = _passwordHashService.CheckPasswordHash(hashInfo.Hash, password);
            switch(checkHashResult)
            {
                case CheckPaswordHashResult.DoesNotMatch:
                    _logger.LogDebug("Password does not match");
                    status.PasswordIncorrect = true;

                    var currentFailedAttemptCount = hashInfo.FailedAttemptCount + 1;
                    if (currentFailedAttemptCount >= _idProviderConfig.MaxPasswordFailedAttempts)
                    {                        
                        var lockUntil = DateTime.UtcNow.AddMinutes(_idProviderConfig.TempLockPasswordMinutes);
                        _logger.LogDebug("Locking password until {0} (UTC)", lockUntil);
                        if(_idProviderConfig.ResetFailedAttemptCountOnTempLock)
                        {
                            // if not reset, the next failure after a lockout initiates another lockout period
                            currentFailedAttemptCount = 0;
                        }
                        await _passwordHashStore.TempLockPasswordHashAsync(user.SubjectId, lockUntil, currentFailedAttemptCount);

                        status.TemporarilyLocked = true;
                        status.AddError("Password is temporarily locked.", HttpStatusCode.Unauthorized);
                        return status;
                    }
                    else
                    {
                        _logger.LogDebug("Updating failed attempt count");
                        await _passwordHashStore.UpdatePasswordHashFailureCountAsync(user.SubjectId, hashInfo.FailedAttemptCount + 1);
                        status.AddError("Password was incorrect.", HttpStatusCode.Unauthorized);
                        return status;
                    }
                case CheckPaswordHashResult.MatchesNeedsRehash:
                    _logger.LogDebug("Rehashing password");
                    var newHash = _passwordHashService.HashPassword(password);
                    await _passwordHashStore.UpdatePasswordHashAsync(user.SubjectId, newHash);
                    status.AddSuccess("Password was correct.");
                    return status;
                case CheckPaswordHashResult.Matches:
                    _logger.LogDebug("Password matches");
                    status.AddSuccess("Password was correct.");
                    return status;
                default:
                    // this should never happen
                    status.AddError("An unexpected error occurred.");
                    return status;
            }
        }

        private bool PasswordIsStrongEnough(string password)
        {
            return password?.Length >= _idProviderConfig.MinimumPasswordLength;
        }

        public async Task<Response<DateTime>> PasswordLastChangedAsync(string uniqueIdentifier)
        {
            var response = await _passwordHashStore.GetPasswordHashAsync(uniqueIdentifier);
            if(response.HasError)
            {
                return Response.StatusOnly<DateTime>(response.Status);
            }
            return Response.Success(response.Result.LastChangedUTC, "Password last changed date found.");
        }
    }
}
