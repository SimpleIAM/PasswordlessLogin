// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.UI.Account
{
    public class SetPasswordModel
    {
        [Required]
        [RegularExpression(@" *[0-9]{6} *", ErrorMessage = "Enter a 6-digit number")]
        public string OneTimeCode { get; set; }

        [Required]
        public string NewPassword { get; set; }

        public int MinimumPasswordStrengthInBits { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }

        public string NextUrl { get; set; }
    }
}
