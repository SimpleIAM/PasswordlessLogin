// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class AuthenticateLongCodeInputModel
    {
        [Required(ErrorMessage = "{0} is required.")]
        public string LongCode { get; set; }
    }
}
