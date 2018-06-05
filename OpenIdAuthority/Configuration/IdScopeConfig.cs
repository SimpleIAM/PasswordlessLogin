// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public class IdScopeConfig
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string[] ClaimTypes { get; set; }
        public bool Required { get; set; }
    }
}
