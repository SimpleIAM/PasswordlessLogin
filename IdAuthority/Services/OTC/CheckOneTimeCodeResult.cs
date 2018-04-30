// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.IdAuthority.Services.OTC
{
    public enum CheckOneTimeCodeResult
    {
        Verified,
        Expired,
        CodeIncorrect,
        ShortCodeLocked,
        NotFound,
        ServiceFailure,
    }
}
