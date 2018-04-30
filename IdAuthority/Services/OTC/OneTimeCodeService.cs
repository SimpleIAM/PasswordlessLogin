// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleIAM.IdAuthority.Entities;

namespace SimpleIAM.IdAuthority.Services.OTC
{
    public class OneTimeCodeService : IOneTimeCodeService
    {
        private IdAuthorityDbContext _context;

        public OneTimeCodeService(IdAuthorityDbContext context)
        {
            _context = context;
        }

        public async Task<OneTimeCode> CreateOneTimeCodeAsync(string email, TimeSpan validity, string redirectUrl = null)
        {
            await UseOneTimeCodeAsync(email); // remove existing otc, if any

            var rngProvider = new RNGCryptoServiceProvider();
            var byteArray = new byte[8];
            rngProvider.GetBytes(byteArray);
            var randomNumber = BitConverter.ToUInt64(byteArray, 0);
            var code = (randomNumber % 1000000).ToString("000000");

            var otc = new OneTimeCode()
            {
                OTC = code,
                ExpiresUTC = DateTime.UtcNow.Add(validity),
                LinkCode = randomNumber.ToString(),
                Email = email,
                RedirectUrl = redirectUrl
            };

            await _context.AddAsync(otc);
            await _context.SaveChangesAsync();

            return otc;
        }

        public async Task<OneTimeCode> UseOneTimeLinkAsync(string linkCode)
        {
            var otc = await _context.OneTimeCodes.SingleOrDefaultAsync(x => x.LinkCode == linkCode);
            if(otc != null)
            {
                await DeleteOneTimeCodeAsync(otc);
            }
            return otc;
        }

        public async Task<OneTimeCode> UseOneTimeCodeAsync(string email)
        {
            var otc = await _context.OneTimeCodes.SingleOrDefaultAsync(x => x.Email == email);
            if (otc != null)
            {
                await DeleteOneTimeCodeAsync(otc);
            }
            return otc;
        }

        public async Task DeleteOneTimeCodeAsync(OneTimeCode oneTimeCode)
        {
            _context.Remove(oneTimeCode);
            await _context.SaveChangesAsync();
        }
    }
}
