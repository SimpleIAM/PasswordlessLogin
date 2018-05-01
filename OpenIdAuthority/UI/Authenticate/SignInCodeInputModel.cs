// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    public class SignInCodeInputModel
    {
        [Required]
        [EmailAddress]
        [RegularExpression(@".+\@.+\..+", ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@" *[0-9]{6} *", ErrorMessage = "Enter a 6-digit number")]
        public string OneTimeCode { get; set; }

        public int? SessionLengthMinutes { get; set; }

        [RegularExpression("^$", ErrorMessage = "Leave blank unless you're a spambot")]
        public string LeaveBlank { get; set; }
    }
}
