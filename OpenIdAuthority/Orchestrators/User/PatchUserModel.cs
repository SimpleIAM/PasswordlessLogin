// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleIAM.OpenIdAuthority.Orchestrators
{
    public class PatchUserModel
    {
        public string SubjectId { get; set; }

        public ILookup<string, string> Properties { get; set; }
    }
}
