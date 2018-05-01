// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.IdAuthority.UI.Account
{
    public class RemovePasswordModel
    {
        public bool GetOneTimeCode { get; set; }

        [Required]
        [RegularExpression(@" *[0-9]{6} *", ErrorMessage = "Enter a 6-digit number")]
        public string OneTimeCode { get; set; }
    }
}
