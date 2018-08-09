// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;

namespace SimpleIAM.PasswordlessLogin.Entities
{
    public class PasswordHash
    {
        public string SubjectId { get; set; }

        public string Hash { get; set; }

        public DateTime LastChangedUTC { get; set; }

        public int FailedAttemptCount { get; set; }

        public DateTime? TempLockUntilUTC { get; set; }
    }
}
