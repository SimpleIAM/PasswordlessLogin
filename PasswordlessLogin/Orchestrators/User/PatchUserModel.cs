// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Linq;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class PatchUserModel
    {
        public ILookup<string, string> Properties { get; set; }
    }
}
