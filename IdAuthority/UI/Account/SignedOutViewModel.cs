// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.IdAuthority.UI.Account
{
    public class SignedOutViewModel
    {
        public string AppName { get; set; }
        public string PostLogoutRedirectUri { get; set; }
        public string SignOutIFrameUrl { get; set; }
    }
}
