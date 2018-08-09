// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Stores
{
    public interface IAppStore
    {
        IEnumerable<App> GetApps();
    }
}
