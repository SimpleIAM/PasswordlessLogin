// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Models;

namespace SimpleIAM.OpenIdAuthority.Services.OTC
{
    public class GetOneTimeCodeResponse
    {
        public GetOneTimeCodeResponse(GetOneTimeCodeResult result)
        {
            Result = result;            
        }

        public GetOneTimeCodeResult Result { get; set; }
        public string ClientNonce { get; set; }
        public string ShortCode { get; set; }
        public string LongCode { get; set; }
    }
}
