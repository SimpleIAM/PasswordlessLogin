// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services.Email
{
    public class EmailTemplate
    {
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
