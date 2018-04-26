// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.IdAuthority.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.IdAuthority.Stores
{
    public interface IAppStore
    {
        IEnumerable<App> GetApps();
    }
}
