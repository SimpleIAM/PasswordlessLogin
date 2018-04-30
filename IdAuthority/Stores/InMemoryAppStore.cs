// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SimpleIAM.IdAuthority.Models;

namespace SimpleIAM.IdAuthority.Stores
{
    public class InMemoryAppStore : IAppStore
    {
        private readonly IEnumerable<App> _apps;
        public InMemoryAppStore(IEnumerable<App> apps)
        {
            _apps = apps;
        }
        public IEnumerable<App> GetApps()
        {
            return _apps;
        }
    }
}
