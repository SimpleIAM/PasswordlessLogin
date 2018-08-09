// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;

namespace SimpleIAM.PasswordlessLogin.Models
{
    public class AuthorizedDevice
    {
        public int RecordId { get; set; }
        public string Description { get; set; }
        public DateTime AddedOn { get; set; }
    }
}
