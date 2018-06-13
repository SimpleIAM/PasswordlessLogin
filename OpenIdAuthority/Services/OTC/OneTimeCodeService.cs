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
        private readonly IAuthorizedDeviceService _authorizedDeviceService;

        public OneTimeCodeService(
            IOneTimeCodeStore oneTimeCodeStore,
            IMessageService messageService,
            IAuthorizedDeviceService authorizedDeviceService
            )
        {
            _oneTimeCodeStore = oneTimeCodeStore;
            _messageService = messageService;
            _authorizedDeviceService = authorizedDeviceService;
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string longCode, string deviceId, string clientNonce)
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
            return await CheckAdditionalFactorsAsync(otc, deviceId, clientNonce);
        }

        public async Task<CheckOneTimeCodeResponse> CheckOneTimeCodeAsync(string sentTo, string shortCode, string deviceId, string clientNonce)
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
                    return await CheckAdditionalFactorsAsync(otc, deviceId, clientNonce);
                }
            }

            await _oneTimeCodeStore.UpdateOneTimeCodeFailureAsync(sentTo, otc.FailedAttemptCount + 1);
            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.CodeIncorrect);
        }

        private async Task<CheckOneTimeCodeResponse> CheckAdditionalFactorsAsync(OneTimeCode otc, string deviceId, string clientNonce)
        {
            await _oneTimeCodeStore.ExpireOneTimeCodeAsync(otc.SentTo);

            if (await _authorizedDeviceService.DeviceIsAuthorized(otc.SentTo, deviceId))
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.VerifiedOnAuthorizedDevice, otc.SentTo, otc.RedirectUrl);
            }

            if (FastHashService.ValidateHash(otc.ClientNonceHash, clientNonce, otc.SentTo))
            {
                return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.VerifiedOnNewDevice, otc.SentTo, otc.RedirectUrl);
            }
            // if code is correct, but the device id and client nonce are incorrect or missing, expire the code and return Expired
            return new CheckOneTimeCodeResponse(CheckOneTimeCodeResult.Expired, otc.SentTo, otc.RedirectUrl);
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
