// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace SimpleIAM.OpenIdAuthority.Services
{
    public static class FastHashService
    {
        public static string GetHash(string input, string salt = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt ?? "")))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }
        public static bool ValidateHash(string hash, string input, string salt = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            var test = GetHash(input, salt);
            return TimeConstantEquals(hash, test);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool TimeConstantEquals(string left, string right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }
            var length = left.Length;
            int bitDiff = 0;
            for (var i = 0; i < length; i++)
            {
                bitDiff = bitDiff | (left[i] - right[i]);
            }
            return bitDiff == 0;
        }
    }
}
