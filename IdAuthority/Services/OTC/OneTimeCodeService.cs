// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.IdAuthority.Models;
using SimpleIAM.IdAuthority.Services.Email;
using SimpleIAM.IdAuthority.Services.Password;
using SimpleIAM.IdAuthority.Stores;

namespace SimpleIAM.IdAuthority.Services.OTC
{
    public class OneTimeCodeService : IOneTimeCodeService
    {
        private readonly IOneTimeCodeStore _oneTimeCodeStore;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IUrlHelper _urlHelper;
        private readonly IHttpContextAccessor _httpContext;

        public OneTimeCodeService(
            IOneTimeCodeStore oneTimeCodeStore,
            IPasswordHashService passwordHashService,
            IEmailTemplateService emailTemplateService,
            IUrlHelper urlHelper,
            IHttpContextAccessor httpContext
            )
        {
            _oneTimeCodeStore = oneTimeCodeStore;
            _passwordHashService = passwordHashService;
            _emailTemplateService = emailTemplateService;
            _urlHelper = urlHelper;
            _httpContext = httpContext;
        }

        public async Task<SendOneTimeCodeResult> SendOneTimeCodeAsync(string sendTo, TimeSpan validity)
        {
            return await SendOneTimeCodeInternalAsync("OneTimeCode", sendTo, validity);
        }

        public async Task<SendOneTimeCodeResult> SendOneTimeCodeAndLinkAsync(string sendTo, TimeSpan validity, string redirectUrl = null)
        {
            return await SendOneTimeCodeInternalAsync("SignInWithEmail", sendTo, validity, redirectUrl);
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

        private async Task<SendOneTimeCodeResult> SendOneTimeCodeInternalAsync(string template, string sendTo, TimeSpan validity, string redirectUrl = null)
        {
            var otc = await _oneTimeCodeStore.GetOneTimeCodeAsync(sendTo);
            if (otc?.ExpiresUTC > DateTime.UtcNow.AddMinutes(2))
            {
                // if they locked the last code, they have to wait until it is almost expired
                // if they didn't recieve the last code, unfortunately they still need to wait. We can't resent the code
                // because it is hashed and we don't know what it is.
                return SendOneTimeCodeResult.TooManyRequests;
            }

            var rngProvider = new RNGCryptoServiceProvider();
            var byteArray = new byte[8];
            rngProvider.GetBytes(byteArray);
            var longCode = BitConverter.ToUInt64(byteArray, 0);
            var longCodeHash = GetFastHash(longCode.ToString());
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
            await _oneTimeCodeStore.AddOneTimeCodeAsync(otc);

            if (sendTo.Contains("@")) // todo: have a better email check?
            {
                var link = _urlHelper.Action("SignInLink", "Authenticate", new { longCode = longCode.ToString() }, _httpContext.HttpContext.Request.Scheme);
                var fields = new Dictionary<string, string>()
                {
                    { "link", link },
                    { "one_time_code", shortCode }
                };
                await _emailTemplateService.SendEmailAsync(template, sendTo, fields);
                return SendOneTimeCodeResult.Sent;
            }
            else
            {
                return SendOneTimeCodeResult.InvalidRequest; // non-email addresses not implemented
                // todo: if valid phone number, send shortcode to phone number via SMS
            }
        }

        private string GetFastHash(string longCode)
        {
            return longCode.Sha256();
        }
    }
}
