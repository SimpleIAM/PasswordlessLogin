// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public enum CheckPasswordResult
    {
        Success,
        // intentionally leaving out SuccessExpired since there is no good reason to expire strong passwords that are adequately protected. Weak passwords can be detected at authentication
        PasswordIncorrect, // if the password was incorrect AND the password is now locked, use TemporarilyLocked
        TemporarilyLocked,
        NotFound,
        ServiceFailure,
    }
}
