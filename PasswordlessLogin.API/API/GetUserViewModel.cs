// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleIAM.PasswordlessLogin.API
{
    public class GetUserViewModel
    {
        [JsonPropertyName("sub")]
        public string SubjectId { get; set; }
        public string Email { get; set; }
        
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalProperties { get; set; }        
    }
}
