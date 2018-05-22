// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SimpleIAM.OpenIdAuthority.API
{
    public class SendPasswordResetMessageInputModel
    {
        public string ApplicationId { get; set; }

        [Required]
        public string Username { get; set; }

        public string NextUrl { get; set; }
    }
}
