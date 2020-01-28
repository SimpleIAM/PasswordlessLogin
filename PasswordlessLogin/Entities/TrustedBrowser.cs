// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;

namespace SimpleIAM.PasswordlessLogin.Entities
{
    public class TrustedBrowser
    {
        public int Id { get; set; }
        public string SubjectId { get; set; }
        public string BrowserIdHash { get; set; }
        public string Description { get; set; }
        public DateTime AddedOn { get; set; }
    }
}
