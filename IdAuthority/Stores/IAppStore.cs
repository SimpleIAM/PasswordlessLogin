// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.IdAuthority.Models;
using System.Collections.Generic;

namespace SimpleIAM.IdAuthority.Stores
{
    public interface IAppStore
    {
        IEnumerable<App> GetApps();
    }
}
