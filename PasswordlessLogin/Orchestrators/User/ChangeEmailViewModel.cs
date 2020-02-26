// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class ChangeEmailViewModel
    {
        [Display(Name = "Old Email")]
        public string OldEmail { get; set; }

        [Display(Name = "New Email")]
        public string NewEmail { get; set; }
    }
}
