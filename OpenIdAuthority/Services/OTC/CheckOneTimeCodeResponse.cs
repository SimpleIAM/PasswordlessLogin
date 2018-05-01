// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Services.OTC
{
    public class CheckOneTimeCodeResponse
    {
        public CheckOneTimeCodeResponse(CheckOneTimeCodeResult result, string sentTo = null, string redirectUrl = null)
        {
            Result = result;
            SentTo = sentTo;
            RedirectUrl = redirectUrl;
        }

        public CheckOneTimeCodeResult Result { get; set; }
        public string SentTo { get; set; }
        public string RedirectUrl { get; set; }
    }
}
