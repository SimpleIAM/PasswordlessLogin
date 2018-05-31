// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    public class SignInInputModel
    {
        [Required]
        public string Username { get; set; }

        public string Password { get; set; }

        public bool StaySignedIn { get; set; }
        
        public string NextUrl { get; set; }

        public string LeaveBlank { get; set; }

        public string Action { get; set; }
    }
}
