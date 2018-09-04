﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Stores;


namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public class OneTimeCodeService : IOneTimeCodeService
    {
        private readonly ILogger _logger;
        private readonly IOneTimeCodeStore _oneTimeCodeStore;
        private readonly IMessageService _messageService;

        public OneTimeCodeService(
            ILogger<OneTimeCodeService> logger,
            IOneTimeCodeStore oneTimeCodeStore,
            IMessageService messageService
            )
        {
            _logger = logger;
            _oneTimeCodeStore = oneTimeCodeStore;
            _messageService = messageService;
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string longCode, string clientNonce)
        {
            _logger.LogTrace("Checking long code");

            if(string.IsNullOrEmpty(longCode) || longCode.Length > PasswordlessLoginConstants.OneTimeCode.LongCodeMaxLength )
            {
                _logger.LogError("The long code provided had an invalid format");
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.CodeIncorrect);
            }

            var otc = await _oneTimeCodeStore.GetOneTimeCodeByLongCodeAsync(longCode);

            if(otc == null)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.NotFound);
            }
            if(otc.ExpiresUTC < DateTime.UtcNow)
            {
                _logger.LogDebug("The one time code has expired");
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Expired);
            }
            return await ExpireTokenAndValidateNonceAsync(otc, clientNonce);
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string sentTo, string shortCode, string clientNonce)
        {
            _logger.LogTrace("Checking short code");

            var otc = await _oneTimeCodeStore.GetOneTimeCodeAsync(sentTo);
            if (otc == null)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.NotFound);
            }
            if (otc.ExpiresUTC < DateTime.UtcNow)
            {
                _logger.LogDebug("The one time code has expired");
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Expired);
            }

            if (!string.IsNullOrEmpty(shortCode) && shortCode.Length == PasswordlessLoginConstants.OneTimeCode.ShortCodeLength)
            {
                if (otc.FailedAttemptCount >= PasswordlessLoginConstants.OneTimeCode.MaxFailedAttemptCount)
                {
                    // maximum of 3 attempts during code validity period to prevent guessing attacks
                    // long code remains valid, preventing account lockout attacks (and giving a fumbling but valid user another way in)
                    _logger.LogDebug("The one time code is locked (too many failed attempts to use it)");
                    return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.ShortCodeLocked); 
                }
                if (shortCode == otc.ShortCode)
                {
                    _logger.LogDebug("The one time code matches");
                    return await ExpireTokenAndValidateNonceAsync(otc, clientNonce);
                }
            }
            else
            {
                _logger.LogDebug("The one time code was missing or was the wrong length");
            }

            _logger.LogDebug("Updating failure count for one time code");
            await _oneTimeCodeStore.UpdateOneTimeCodeFailureAsync(sentTo, otc.FailedAttemptCount + 1);
            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.CodeIncorrect);
        }

        private async Task<CheckOneTimeCodeResponse> ExpireTokenAndValidateNonceAsync(OneTimeCode otc, string clientNonce)
        {
            _logger.LogTrace("Validating nonce");

            _logger.LogDebug("Expiring the token so it cannot be used again and so a new token can be generated");
            await _oneTimeCodeStore.ExpireOneTimeCodeAsync(otc.SentTo);

            if (FastHashService.ValidateHash(otc.ClientNonceHash, clientNonce, otc.SentTo))
            {
                _logger.LogDebug("Client nonce was valid");
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.VerifiedWithNonce, otc.SentTo, otc.RedirectUrl);
            }

            _logger.LogDebug("Client nonce was missing or invalid");
            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.VerifiedWithoutNonce, otc.SentTo, otc.RedirectUrl);
        }

        public async Task<GetOneTimeCodeResponse> GetOneTimeCodeAsync(string sendTo, TimeSpan validity, string redirectUrl = null)
        {
            var otc = await _oneTimeCodeStore.GetOneTimeCodeAsync(sendTo);

            if (otc != null && 
                otc.ExpiresUTC > DateTime.UtcNow.AddMinutes(PasswordlessLoginConstants.OneTimeCode.IssueNewCodeIfValidityLessThanXMinutes) && 
                otc.ExpiresUTC < DateTime.UtcNow.AddMinutes(PasswordlessLoginConstants.OneTimeCode.DefaultValidityMinutes))
            {
                _logger.LogDebug("A once time code exists that has enough time left to use");
                // existing code has at least X minutes of validity remaining, so resend it
                // if more than default validity (e.g. first code sent to new user), user could accidentally 
                // lock the code and not be able to confirm or access the account (terrible UX)
                if (otc.SentCount >= PasswordlessLoginConstants.OneTimeCode.MaxResendCount)
                {
                    _logger.LogDebug("The existing one time code has been sent too many times");
                    return new GetOneTimeCodeResponse(GetOneTimeCodeResult.TooManyRequests);
                }

                _logger.LogDebug("Updating the record of how many times the code has been sent");
                await _oneTimeCodeStore.UpdateOneTimeCodeSentCountAsync(sendTo, otc.SentCount + 1, redirectUrl);

                _logger.LogDebug("Returning the still valid code without a client nonce, which can only be delivered once.");
                return new GetOneTimeCodeResponse(GetOneTimeCodeResult.Success)
                {
                    ClientNonce = null, // only given out when code is first generated
                    ShortCode = otc.ShortCode,
                    LongCode = otc.LongCode
                };
            }

            _logger.LogDebug("Generating a new one time code, link, and client nonce.");
            var rngProvider = new RNGCryptoServiceProvider();
            var byteArray = new byte[8];

            rngProvider.GetBytes(byteArray);
            var clientNonceUInt = BitConverter.ToUInt64(byteArray, 0);
            var clientNonce = clientNonceUInt.ToString();
            var clientNonceHash = FastHashService.GetHash(clientNonce, sendTo);

            rngProvider.GetBytes(byteArray);
            var longCodeUInt = BitConverter.ToUInt64(byteArray, 0);
            var longCode = longCodeUInt.ToString();

            var shortCode = (longCodeUInt % 1000000).ToString("000000");

            otc = new OneTimeCode()
            {
                SentTo = sendTo,
                ClientNonceHash = clientNonceHash,
                ShortCode = shortCode,
                ExpiresUTC = DateTime.UtcNow.Add(validity),
                LongCode = longCode,
                RedirectUrl = redirectUrl,
                FailedAttemptCount = 0,
                SentCount = 1
            };
            await _oneTimeCodeStore.RemoveOneTimeCodeAsync(sendTo);
            var codeSaved = await _oneTimeCodeStore.AddOneTimeCodeAsync(otc);
            if (!codeSaved)
            {
                _logger.LogError("Failed to store the code.");
                return new GetOneTimeCodeResponse(GetOneTimeCodeResult.ServiceFailure);
            }

            return new GetOneTimeCodeResponse(GetOneTimeCodeResult.Success)
            {
                ClientNonce = clientNonce,
                ShortCode = shortCode,
                LongCode = longCode
            };
        }
    }
}