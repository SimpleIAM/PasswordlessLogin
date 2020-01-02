// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.API
{
    public class SetPasswordInputModel
    {
        public string ApplicationId { get; set; }

        public string OldPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}
