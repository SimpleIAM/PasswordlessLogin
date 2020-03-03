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
            if (email == null || email.Length > 254 || email.IndexOf('@') < 1)
            {
                return false;
            }

            // Require at least a 2-letter extension after the @
            // (validation below allows extensionless domains like localhost)
            // Sample:  a@b.cd : Length = 6, LastIndexOf('.') = 3, IndexOf('@') = 1
            // Index:   012345
            if (email.LastIndexOf('.') < (email.IndexOf('@') + 1) || email.LastIndexOf('.') > (email.Length - 3))
            {
                return false;
            }

            // Use .NET's built-in validation
            if (!new EmailAddressAttribute().IsValid(email))
            {
                return false;
            }

            return true;
        }
    }
}
