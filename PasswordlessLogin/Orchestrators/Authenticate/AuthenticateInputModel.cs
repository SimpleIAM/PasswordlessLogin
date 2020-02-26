// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class AuthenticateInputModel
    {
        [Display(Name = "Username")]
        [Required(ErrorMessage = "{0} is required.")]
        public string Username { get; set; }

        [Display(Name = "One Time Code")]
        [Required(ErrorMessage = "{0} is required.")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Enter a 6-digit number")]
        public string OneTimeCode { get; set; }

        [Display(Name = "Stay Signed In")]
        public bool StaySignedIn { get; set; }
    }
}
