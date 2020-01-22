// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;

namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public class CheckOneTimeCodeResult
    {
        public CheckOneTimeCodeResult(OneTimeCode otc)
        {
            SentTo = otc.SentTo;
            RedirectUrl = otc.RedirectUrl;
        }

        public string SentTo { get; set; }
        public string RedirectUrl { get; set; }
    }
}
