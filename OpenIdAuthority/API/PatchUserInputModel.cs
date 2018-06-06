// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SimpleIAM.OpenIdAuthority.API
{
    public class PatchUserInputModel
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> Properties { get; set; }        
    }
}
