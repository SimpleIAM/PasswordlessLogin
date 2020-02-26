// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class RegisterInputModel
    {
        public string ApplicationId { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "{0} is required.")]
        [EmailAddress]
        [RegularExpression(PasswordlessLoginConstants.BasicEmailRegexPattern, ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        // Optional password
        [Display(Name = "Password")]
        public string Password { get; set; }

        public Dictionary<string, string> Claims {get; set;}

        public bool SetPassword { get; set; } = false;

        public string NextUrl { get; set; }
    }
}
