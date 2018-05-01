// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority
{
    public static class MappingExtensions
    {
        public static Entities.OneTimeCode ToEntity(this Models.OneTimeCode source)
        {
            return new Entities.OneTimeCode()
            {
                SentTo = source.SentTo,
                ExpiresUTC = source.ExpiresUTC,
                FailedAttemptCount = source.FailedAttemptCount,
                LongCodeHash = source.LongCodeHash,
                ShortCodeHash = source.ShortCodeHash,
                RedirectUrl = source.RedirectUrl,
            };
        }

        public static Models.OneTimeCode ToModel(this Entities.OneTimeCode source)
        {
            return new Models.OneTimeCode()
            {
                SentTo = source.SentTo,
                ExpiresUTC = source.ExpiresUTC,
                FailedAttemptCount = source.FailedAttemptCount,
                LongCodeHash = source.LongCodeHash,
                ShortCodeHash = source.ShortCodeHash,
                RedirectUrl = source.RedirectUrl,
            };
        }

        public static Models.PasswordHash ToModel(this Entities.PasswordHash source)
        {
            return new Models.PasswordHash()
            {
                SubjectId = source.SubjectId,
                Hash = source.Hash,
                LastChangedUTC = source.LastChangedUTC,
                FailedAttemptCount = source.FailedAttemptCount,
                TempLockUntilUTC = source.TempLockUntilUTC,
            };
        }
    }
}
