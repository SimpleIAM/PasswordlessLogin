// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    public class SignInInputModel
    {
        [Required]
        [EmailAddress]
        [RegularExpression(@".+\@.+\..+", ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [RegularExpression("^$", ErrorMessage = "Leave blank unless you're a spambot")]
        public string LeaveBlank { get; set; }
    }
}
