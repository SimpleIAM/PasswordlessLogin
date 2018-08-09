// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class AuthenticatePasswordInputModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public bool StaySignedIn { get; set; }
        
        public string NextUrl { get; set; }
    }
}
