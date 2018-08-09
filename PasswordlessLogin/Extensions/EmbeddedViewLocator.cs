// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc.Razor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleIAM.PasswordlessLogin
{
    public class EmbeddedViewLocator : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            // {2} is area, {1} is controller, {0} is the action
            string[] embeddedLocations = new string[] {
                "/{1}.{0}.cshtml",
                "/Shared.{0}.cshtml"
            };
            return viewLocations.Union(embeddedLocations); // Add embedded locations after the default ones
        }
    }
}
