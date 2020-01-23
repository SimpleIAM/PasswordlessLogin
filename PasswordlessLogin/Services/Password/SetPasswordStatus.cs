// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using StandardResponse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public class SetPasswordStatus : Status
    {
        public bool PasswordDoesNotMeetStrengthRequirements { get; set; }
    }
}
