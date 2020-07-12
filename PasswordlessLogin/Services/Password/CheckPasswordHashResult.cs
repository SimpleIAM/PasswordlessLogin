// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public enum CheckPasswordHashResult
    {
        Matches,
        MatchesNeedsRehash,
        DoesNotMatch
    }
}