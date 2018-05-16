// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Services.OTC
{
    public class SendOneTimeCodeResponse
    {
        public SendOneTimeCodeResponse(SendOneTimeCodeResult result, string messageForEndUser = null)
        {
            Result = result;
            MessageForEndUser = messageForEndUser;
        }

        public SendOneTimeCodeResult Result { get; set; }
        public string MessageForEndUser { get; set; }
    }
}
