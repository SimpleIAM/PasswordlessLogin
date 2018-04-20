// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.IdAuthority.UI.Account
{
    public class SignInInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Password { get; set; }

        public int? SessionLengthMinutes { get; set; }

        [RegularExpression("^$", ErrorMessage = "Leave blank if you aren't a spam-bot")]
        public string LeaveBlank { get; set; }
    }
}
