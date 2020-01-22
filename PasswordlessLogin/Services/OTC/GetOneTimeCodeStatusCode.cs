// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public enum GetOneTimeCodeStatusCode
    {
        Success,
        TooManyRequests, // there is a valid code that hasn't expired
        ServiceFailure,
    }
}
