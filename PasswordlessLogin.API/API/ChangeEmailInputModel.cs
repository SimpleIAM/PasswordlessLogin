// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.API
{
    public class ChangeEmailInputModel
    {
        public string ApplicationId { get; set; }
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(PasswordlessLoginConstants.BasicEmailRegexPattern, ErrorMessage = "Enter a valid email address")]
        public string NewEmail { get; set; }
    }
}
