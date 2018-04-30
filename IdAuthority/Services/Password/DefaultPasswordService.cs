// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.IdAuthority.Stores;
using System;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Services.Password
{
    public class DefaultPasswordService : IPasswordService
    {
        private readonly IPasswordHashService _passwordHashService;
        private readonly IPasswordHashStore _passwordHashStore;
        public DefaultPasswordService(
            IPasswordHashService passwordHashService,
            IPasswordHashStore passwordHashStore
            )
        {
            _passwordHashService = passwordHashService;
            _passwordHashStore = passwordHashStore;
        }

        public bool CanSetPassword => true;

        public bool CanChangePassword => true;

        public bool CanRemovePassword => true;

        public string UniqueIdentifierClaimType => "sub";

        public async Task<ChangePasswordResult> ChangePasswordAsync(string uniqueIdentifier, string oldPassword, string newPassword)
        {
            if (!PasswordIsStrongEnough(newPassword))
            {
                return ChangePasswordResult.NewPasswordDoesNotMeetStrengthRequirements;
            }
            var checkOldPaswordResult = await CheckPasswordAsync(uniqueIdentifier, oldPassword);
            switch(checkOldPaswordResult)
            {
                case CheckPasswordResult.PasswordIncorrect:
                case CheckPasswordResult.NotFound:
                    return ChangePasswordResult.OldPasswordIncorrect;
                case CheckPasswordResult.ServiceFailure:
                    return ChangePasswordResult.ServiceFailure;
                case CheckPasswordResult.TemporarilyLocked:
                    // this case is hairy... we probably need to validate the hash via a common method
                    return ChangePasswordResult.ServiceFailure;
                case CheckPasswordResult.Success:
                    var newHash = _passwordHashService.HashPassword(newPassword);
                    var success = await _passwordHashStore.UpdatePasswordHashAsync(uniqueIdentifier, newHash);
                    //todo: reset lockout and failed count, or should that be done in the store?
                    return success ? ChangePasswordResult.Success : ChangePasswordResult.ServiceFailure;
                default:
                    // this should never happen
                    return ChangePasswordResult.ServiceFailure;
            }
        }

        public async Task<RemovePasswordResult> RemovePasswordAsync(string uniqueIdentifier)
        {
            var result = await _passwordHashStore.RemovePasswordHashAsync(uniqueIdentifier);
            return result ? RemovePasswordResult.Success : RemovePasswordResult.ServiceFailure;
        }

        public async Task<SetPasswordResult> SetPasswordAsync(string uniqueIdentifier, string password)
        {
            if(!PasswordIsStrongEnough(password))
            {
                return SetPasswordResult.PasswordDoesNotMeetStrengthRequirements;
            }
            var hash = _passwordHashService.HashPassword(password);
            var result = await _passwordHashStore.AddPasswordHashAsync(uniqueIdentifier, hash);
            return result ? SetPasswordResult.Success : SetPasswordResult.ServiceFailure;
        }

        public async Task<CheckPasswordResult> CheckPasswordAsync(string uniqueIdentifier, string password)
        {
            var hashInfo = await _passwordHashStore.GetPasswordHashAsync(uniqueIdentifier);
            if(hashInfo == null)
            {
                return CheckPasswordResult.NotFound;
            }
            if(hashInfo.TempLockUntilUTC > DateTime.UtcNow)
            {
                return CheckPasswordResult.TemporarilyLocked;
            }
            var checkHashResult = _passwordHashService.CheckPasswordHash(hashInfo.Hash, password);
            switch(checkHashResult)
            {
                case CheckPaswordHashResult.DoesNotMatch:
                    if (hashInfo.FailedAttemptCount > 3) // todo, get from settings
                    {
                        var lockUntil = DateTime.UtcNow.AddMinutes(5); // todo: get from settings
                        //todo: consider if failure count should be reset or not. (should first subsequent failure after lockout initiate another lockout period?)
                        await _passwordHashStore.TempLockPasswordHashAsync(uniqueIdentifier, lockUntil);
                        return CheckPasswordResult.TemporarilyLocked;
                    }
                    else
                    {
                        await _passwordHashStore.UpdatePasswordHashFailureAsync(uniqueIdentifier, hashInfo.FailedAttemptCount + 1);
                        return CheckPasswordResult.PasswordIncorrect;
                    }
                case CheckPaswordHashResult.MatchesNeedsRehash:
                    var newHash = _passwordHashService.HashPassword(password);
                    await _passwordHashStore.UpdatePasswordHashAsync(uniqueIdentifier, newHash);
                    return CheckPasswordResult.Success;
                case CheckPaswordHashResult.Matches:
                    return CheckPasswordResult.Success;
                default:
                    // this should never happen
                    return CheckPasswordResult.ServiceFailure;
            }
        }

        private bool PasswordIsStrongEnough(string password)
        {
            //todo: implement an accurate password strength check or a length check based on settings
            return password?.Length > 8;
        }

        public async Task<bool> UserHasPasswordAsync(string uniqueIdentifier)
        {
            var record = await _passwordHashStore.GetPasswordHashAsync(uniqueIdentifier);
            return record != null;
        }
    }
}
