// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class PasswordlessLoginBuilder
    {
        public PasswordlessLoginBuilder(IServiceCollection services, PasswordlessLoginOptions passwordlessLoginOptions)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Options = passwordlessLoginOptions ?? throw new ArgumentNullException(nameof(passwordlessLoginOptions));
        }

        public IServiceCollection Services { get; }

        public PasswordlessLoginOptions Options { get; }
    }
}
