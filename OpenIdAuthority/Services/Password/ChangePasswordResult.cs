// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Services.Password
{
    public enum ChangePasswordResult
    {
        Success,
        OldPasswordIncorrect, // or account not found
        NewPasswordDoesNotMeetStrengthRequirements,
        ServiceFailure,
    }
}
