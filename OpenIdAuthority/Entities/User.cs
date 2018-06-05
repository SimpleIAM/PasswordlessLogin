// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.OpenIdAuthority.Entities
{
    public class User
    {
        public string SubjectId { get; set; }

        public string Email { get; set; }

        public List<UserClaim> Claims { get; set; }
    }
}
