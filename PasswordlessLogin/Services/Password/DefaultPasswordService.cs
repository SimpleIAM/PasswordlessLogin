// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Stores;
using System;
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

        public async Task<RemovePasswordResult> RemovePasswordAsync(string uniqueIdentifier)
        {
            _logger.LogDebug("Removing password for {0}", uniqueIdentifier);
            var result = await _passwordHashStore.RemovePasswordHashAsync(uniqueIdentifier);
            return result ? RemovePasswordResult.Success : RemovePasswordResult.ServiceFailure;
        }

        public async Task<SetPasswordResult> SetPasswordAsync(string uniqueIdentifier, string password)
        {
            if(string.IsNullOrEmpty(uniqueIdentifier))
            {
                throw new ArgumentException($"{nameof(uniqueIdentifier)} is required");
            }
            _logger.LogDebug("Setting password for {0}", uniqueIdentifier);
            if (!PasswordIsStrongEnough(password))
            {
                _logger.LogDebug("Password does not meet strength requirements");
                return SetPasswordResult.PasswordDoesNotMeetStrengthRequirements;
            }
            var hash = _passwordHashService.HashPassword(password);
            if(await RemovePasswordAsync(uniqueIdentifier) == RemovePasswordResult.ServiceFailure)
            {
                return SetPasswordResult.ServiceFailure;
            }
            var result = await _passwordHashStore.AddPasswordHashAsync(uniqueIdentifier, hash);
            return result ? SetPasswordResult.Success : SetPasswordResult.ServiceFailure;
        }

        public async Task<CheckPasswordResult> CheckPasswordAsync(string uniqueIdentifier, string password)
        {
            _logger.LogDebug("Checking password for {0}", uniqueIdentifier);
            var hashInfo = await _passwordHashStore.GetPasswordHashAsync(uniqueIdentifier);
            if(hashInfo == null)
            {
                return CheckPasswordResult.NotFound;
            }
            if(hashInfo.TempLockUntilUTC > DateTime.UtcNow)
            {
                _logger.LogDebug("Password is temporarily locked");
                return CheckPasswordResult.TemporarilyLocked;
            }
            var checkHashResult = _passwordHashService.CheckPasswordHash(hashInfo.Hash, password);
            switch(checkHashResult)
            {
                case CheckPaswordHashResult.DoesNotMatch:
                    _logger.LogDebug("Password does not match");
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
                        await _passwordHashStore.TempLockPasswordHashAsync(uniqueIdentifier, lockUntil, currentFailedAttemptCount);
                        return CheckPasswordResult.TemporarilyLocked;
                    }
                    else
                    {
                        _logger.LogDebug("Updating failed attempt count");
                        await _passwordHashStore.UpdatePasswordHashFailureCountAsync(uniqueIdentifier, hashInfo.FailedAttemptCount + 1);
                        return CheckPasswordResult.PasswordIncorrect;
                    }
                case CheckPaswordHashResult.MatchesNeedsRehash:
                    _logger.LogDebug("Rehashing password");
                    var newHash = _passwordHashService.HashPassword(password);
                    await _passwordHashStore.UpdatePasswordHashAsync(uniqueIdentifier, newHash);
                    return CheckPasswordResult.Success;
                case CheckPaswordHashResult.Matches:
                    _logger.LogDebug("Password matches");
                    return CheckPasswordResult.Success;
                default:
                    // this should never happen
                    return CheckPasswordResult.ServiceFailure;
            }
        }

        private bool PasswordIsStrongEnough(string password)
        {
            return password?.Length >= _idProviderConfig.MinimumPasswordLength;
        }

        public async Task<DateTime?> PasswordLastChangedAsync(string uniqueIdentifier)
        {
            var record = await _passwordHashStore.GetPasswordHashAsync(uniqueIdentifier);
            return record?.LastChangedUTC;
        }
    }
}
