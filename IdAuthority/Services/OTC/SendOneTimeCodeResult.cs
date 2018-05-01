// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.IdAuthority.Services.OTC
{
    public enum SendOneTimeCodeResult
    {
        Sent,
        TooManyRequests, // there is a valid code that hasn't expired
        InvalidRequest,
        ServiceFailure,
    }
}
