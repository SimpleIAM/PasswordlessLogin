// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Models
{
    public class User
    {
        public string SubjectId { get; set; }

        public string Email { get; set; }

        public IEnumerable<UserClaim> Claims { get; set; } = new UserClaim[] { };
    }
}
