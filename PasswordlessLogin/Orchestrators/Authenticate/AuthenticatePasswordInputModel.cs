// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class AuthenticatePasswordInputModel
    {
        [Display(Name = "Username")]
        [Required(ErrorMessage = "{0} is required.")]
        public string Username { get; set; }

        [Display(Name = "Password")]
        [Required(ErrorMessage = "{0} is required.")]
        public string Password { get; set; }

        [Display(Name = "Stay Signed In")]
        public bool StaySignedIn { get; set; }
        
        public string NextUrl { get; set; }
    }
}
