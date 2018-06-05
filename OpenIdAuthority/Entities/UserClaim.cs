// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Entities
{
    public class UserClaim
    {
        public int Id { get; set; }
        public string SubjectId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
