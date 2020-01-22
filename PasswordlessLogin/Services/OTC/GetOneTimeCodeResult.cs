// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;

namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public class GetOneTimeCodeResult
    {
        public string ClientNonce { get; set; }
        public string ShortCode { get; set; }
        public string LongCode { get; set; }
    }
}
