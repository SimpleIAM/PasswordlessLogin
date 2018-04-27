﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.IdAuthority.Services.Password
{
    public enum ChangePasswordResult
    {
        Success,
        OldPasswordIncorrect, // or account not found
        NewPasswordDoesNotMeetStrengthRequirements,
        NotSupported,
        ServiceFailure,
    }
}