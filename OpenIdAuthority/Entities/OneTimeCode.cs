// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;

namespace SimpleIAM.OpenIdAuthority.Entities
{
    public class OneTimeCode
    {
        public string SentTo { get; set; }

        public string ClientNonceHash { get; set; }

        public string ShortCode { get; set; }

        public string LongCode { get; set; }

        public DateTime ExpiresUTC { get; set; }

        public int FailedAttemptCount { get; set; }

        public int SentCount { get; set; }

        public string RedirectUrl { get; set; }
    }
}
