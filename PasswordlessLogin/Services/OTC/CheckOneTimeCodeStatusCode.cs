// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public enum CheckOneTimeCodeStatusCode
    {
        VerifiedWithNonce,
        VerifiedWithoutNonce,
        Expired,
        CodeIncorrect,
        ShortCodeLocked,
        NotFound,
        ServiceFailure,
    }
}
