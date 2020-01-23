// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Helpers
{
    public static class EmailAddressChecker
    {
        public static bool EmailIsValid(string email)
        {
            // basic checks first to mitigate regex DOS attacks
            if (email == null || email.Length > 254 || email.IndexOf("@") < 1)
            {
                return false;
            }
            return new EmailAddressAttribute().IsValid(email);
        }
    }
}
