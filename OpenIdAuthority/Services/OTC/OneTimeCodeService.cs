// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SimpleIAM.OpenIdAuthority.Models;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Stores;


namespace SimpleIAM.OpenIdAuthority.Services.OTC
{
    public class OneTimeCodeService : IOneTimeCodeService
    {
        private readonly IOneTimeCodeStore _oneTimeCodeStore;
        private readonly IMessageService _messageService;

        public OneTimeCodeService(
            IOneTimeCodeStore oneTimeCodeStore,
            IMessageService messageService
            )
        {
            _oneTimeCodeStore = oneTimeCodeStore;
            _messageService = messageService;
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string longCode, string clientNonce)
        {
            if(string.IsNullOrEmpty(longCode) || longCode.Length > 36 )
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.CodeIncorrect);
            }

            var otc = await _oneTimeCodeStore.GetOneTimeCodeByLongCodeAsync(longCode);

            if(otc == null)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.NotFound);
            }
            if(otc.ExpiresUTC < DateTime.UtcNow)
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Expired);
            }
            return await ValidateNonceAsync(otc, clientNonce);
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string sentTo, string shortCode, string clientNonce)
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
                if (otc.FailedAttemptCount >= 3)
                {
                    // maximum of 3 attempts during code validity period to prevent guessing attacks
                    // long code remains valid, preventing account lockout attacks (and giving a fumbling but valid user another way in)
                    return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.ShortCodeLocked); 
                }
                if (shortCode == otc.ShortCode)
                {
                    return await ValidateNonceAsync(otc, clientNonce);
                }
            }

            await _oneTimeCodeStore.UpdateOneTimeCodeFailureAsync(sentTo, otc.FailedAttemptCount + 1);
            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.CodeIncorrect);
        }

        private async Task<CheckOneTimeCodeResponse> ValidateNonceAsync(OneTimeCode otc, string clientNonce)
        {
            await _oneTimeCodeStore.ExpireOneTimeCodeAsync(otc.SentTo);

            if (FastHashService.ValidateHash(otc.ClientNonceHash, clientNonce, otc.SentTo))
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.VerifiedWithNonce, otc.SentTo, otc.RedirectUrl);
            }

            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.VerifiedWithoutNonce, otc.SentTo, otc.RedirectUrl);
        }

        public async Task<GetOneTimeCodeResponse> GetOneTimeCodeAsync(string sendTo, TimeSpan validity, string redirectUrl = null)
        {
            var otc = await _oneTimeCodeStore.GetOneTimeCodeAsync(sendTo);

            if (otc != null && otc.ExpiresUTC > DateTime.UtcNow.AddMinutes(2) && otc.ExpiresUTC < DateTime.UtcNow.AddMinutes(10))
            {
                // existing code has 2-10 minutes of validity remaining, so resend it
                // if less than 2 minutes, user might not have time to use it
                // if more than 10 minutes (e.g. first code sent to new user), user could lock the code and be locked out for a long time
                if (otc.SentCount >= 4)
                {
                    return new GetOneTimeCodeResponse(GetOneTimeCodeResult.TooManyRequests);
                }
                await _oneTimeCodeStore.UpdateOneTimeCodeSentCountAsync(sendTo, otc.SentCount + 1, redirectUrl);
                return new GetOneTimeCodeResponse(GetOneTimeCodeResult.Success)
                {
                    ClientNonce = null, // only given out when code is first generated
                    ShortCode = otc.ShortCode,
                    LongCode = otc.LongCode
                };
            }

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
