﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleIAM.PasswordlessLogin.API
{
    public class PatchUserInputModel
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> Properties { get; set; }        
    }
}
