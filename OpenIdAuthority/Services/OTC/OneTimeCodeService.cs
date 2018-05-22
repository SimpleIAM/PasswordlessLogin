// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IdentityServer4.Models;
using SimpleIAM.OpenIdAuthority.Models;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;

namespace SimpleIAM.OpenIdAuthority.Services.OTC
{
    public class OneTimeCodeService : IOneTimeCodeService
    {
        private readonly IOneTimeCodeStore _oneTimeCodeStore;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IMessageService _messageService;

        public OneTimeCodeService(
            IOneTimeCodeStore oneTimeCodeStore,
            IPasswordHashService passwordHashService,
            IMessageService messageService
            )
        {
            _oneTimeCodeStore = oneTimeCodeStore;
            _passwordHashService = passwordHashService;
            _messageService = messageService;
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string longCode)
        {
            if(string.IsNullOrEmpty(longCode) || longCode.Length > 36 )
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.CodeIncorrect);
            }

            var longCodeHash = GetFastHash(longCode);
            var otc = await _oneTimeCodeStore.GetOneTimeCodeByLongCodeAsync(longCodeHash);

            if(otc == null)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.NotFound);
            }
            if(otc.ExpiresUTC < DateTime.UtcNow)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Expired);
            }

            await _oneTimeCodeStore.ExpireOneTimeCodeAsync(otc.SentTo);
            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Verified, otc.SentTo, otc.RedirectUrl);
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string sentTo, string shortCode)
        {
            var otc = await _oneTimeCodeStore.GetOneTimeCodeAsync(sentTo);
            if (otc == null)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.NotFound);
            }
            if (otc.ExpiresUTC < DateTime.UtcNow)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Expired);
            }

            if (!string.IsNullOrEmpty(shortCode) && shortCode.Length <= 8)
            {
                if (otc.FailedAttemptCount > 1)
                {
                    // maximum of 2 attempts during code validity period to prevent guessing attacks
                    // long code remains valid, preventing account lockout attacks (and giving a fumbling but valid user another way in)
                    return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.ShortCodeLocked); 
                }
                var checkResult = _passwordHashService.CheckPasswordHash(otc.ShortCodeHash, shortCode);
                if (checkResult == CheckPaswordHashResult.Matches || checkResult == CheckPaswordHashResult.MatchesNeedsRehash)
                {
                    await _oneTimeCodeStore.ExpireOneTimeCodeAsync(sentTo);
                    return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Verified, sentTo, otc.RedirectUrl);
                }
            }

            await _oneTimeCodeStore.UpdateOneTimeCodeFailureAsync(sentTo, otc.FailedAttemptCount + 1);
            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.CodeIncorrect);
        }

        public async Task<GetOneTimeCodeResponse> GetOneTimeCodeAsync(string sendTo, TimeSpan validity, string redirectUrl = null)
        {
            var otc = await _oneTimeCodeStore.GetOneTimeCodeAsync(sendTo);
            if (otc?.ExpiresUTC > DateTime.UtcNow.AddMinutes(2))
            {
                // if they locked the last code, they have to wait until it is almost expired
                // if they didn't recieve the last code, unfortunately they still need to wait. We can't resent the code
                // because it is hashed and we don't know what it is.
                return new GetOneTimeCodeResponse(GetOneTimeCodeResult.TooManyRequests);
            }

            var rngProvider = new RNGCryptoServiceProvider();
            var byteArray = new byte[8];
            rngProvider.GetBytes(byteArray);
            var longCode = BitConverter.ToUInt64(byteArray, 0);
            var longCodeString = longCode.ToString();
            var longCodeHash = GetFastHash(longCodeString);
            var shortCode = (longCode % 1000000).ToString("000000");
            var shortCodeHash = _passwordHashService.HashPassword(shortCode); // a fast hash salted with longCodeHash might be a sufficient alternative

            otc = new OneTimeCode()
            {
                SentTo = sendTo,
                ShortCodeHash = shortCodeHash,
                ExpiresUTC = DateTime.UtcNow.Add(validity),
                LongCodeHash = longCodeHash,
                RedirectUrl = redirectUrl,
                FailedAttemptCount = 0,
            };
            await _oneTimeCodeStore.RemoveOneTimeCodeAsync(sendTo);
            var codeSaved = await _oneTimeCodeStore.AddOneTimeCodeAsync(otc);
            if (!codeSaved)
            {
                return new GetOneTimeCodeResponse(GetOneTimeCodeResult.ServiceFailure);
            }

            return new GetOneTimeCodeResponse(GetOneTimeCodeResult.Success)
            {
                ShortCode = shortCode,
                LongCode = longCodeString
            };
        }

        private string GetFastHash(string longCode)
        {
            return longCode.Sha256();
        }
    }
}
