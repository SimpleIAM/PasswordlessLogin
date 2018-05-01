// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Models;
using System.Collections.Generic;

namespace SimpleIAM.OpenIdAuthority.Stores
{
    public interface IAppStore
    {
        IEnumerable<App> GetApps();
    }
}
