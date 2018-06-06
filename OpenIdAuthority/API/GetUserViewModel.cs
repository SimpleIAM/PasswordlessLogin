// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SimpleIAM.OpenIdAuthority.API
{
    public class GetUserViewModel
    {
        [JsonProperty("sub")]
        public string SubjectId { get; set; }
        public string Email { get; set; }
        
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalProperties { get; set; }        
    }
}
