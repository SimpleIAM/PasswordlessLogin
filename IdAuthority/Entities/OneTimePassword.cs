// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.IdAuthority.Entities
{
    public class OneTimePassword
    {
        public string Email { get; set; }

        public string OTP { get; set; }

        public string LinkCode { get; set; }

        public DateTime ExpiresUTC { get; set; }

        public string RedirectUrl { get; set; }
    }
}
