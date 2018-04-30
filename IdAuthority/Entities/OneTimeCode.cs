// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;

namespace SimpleIAM.IdAuthority.Entities
{
    public class OneTimeCode
    {
        public string SentTo { get; set; }

        public string ShortCodeHash { get; set; }

        public string LongCodeHash { get; set; }

        public DateTime ExpiresUTC { get; set; }

        public int FailedAttemptCount { get; set; }

        public string RedirectUrl { get; set; }
    }
}
