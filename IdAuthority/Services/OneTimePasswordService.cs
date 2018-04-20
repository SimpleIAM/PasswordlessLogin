// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimpleIAM.IdAuthority.Entities;

namespace SimpleIAM.IdAuthority.Services
{
    public class OneTimePasswordService : IOneTimePasswordService
    {
        private IdAuthorityDbContext _context;

        public OneTimePasswordService(IdAuthorityDbContext context)
        {
            _context = context;
        }

        public async Task<OneTimePassword> CreateOneTimePasswordAsync(string email, TimeSpan validity, string redirectUrl = null)
        {
            await UseOneTimePasswordAsync(email); // remove existing otp, if any

            var rngProvider = new RNGCryptoServiceProvider();
            var byteArray = new byte[8];
            rngProvider.GetBytes(byteArray);
            var randomNumber = BitConverter.ToUInt64(byteArray, 0);
            var otp = (randomNumber % 1000000).ToString("000000");

            var otc = new OneTimePassword()
            {
                OTP = otp,
                ExpiresUTC = DateTime.UtcNow.Add(validity),
                LinkCode = randomNumber.ToString(),
                Email = email,
                RedirectUrl = redirectUrl
            };

            await _context.AddAsync(otc);
            await _context.SaveChangesAsync();

            return otc;
        }

        public async Task<OneTimePassword> UseOneTimeLinkAsync(string linkCode)
        {
            var otp = await _context.OneTimePasswords.SingleOrDefaultAsync(x => x.LinkCode == linkCode);
            if(otp != null)
            {
                await DeleteOneTimePasswordAsync(otp);
            }
            return otp;
        }

        public async Task<OneTimePassword> UseOneTimePasswordAsync(string email)
        {
            var otp = await _context.OneTimePasswords.SingleOrDefaultAsync(x => x.Email == email);
            if (otp != null)
            {
                await DeleteOneTimePasswordAsync(otp);
            }
            return otp;
        }

        public async Task DeleteOneTimePasswordAsync(OneTimePassword oneTimePassword)
        {
            _context.Remove(oneTimePassword);
            await _context.SaveChangesAsync();
        }
    }
}
