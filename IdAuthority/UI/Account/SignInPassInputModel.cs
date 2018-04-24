// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.IdAuthority.UI.Account
{
    public class SignInPassInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public int? SessionLengthMinutes { get; set; }

        [RegularExpression("^$", ErrorMessage = "Leave blank unless you're a spambot")]
        public string LeaveBlank { get; set; }
    }
}
