// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class PasswordlessLoginApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePasswordlessLogin(this IApplicationBuilder app, IFileProvider webRootFileProvider)
        {
            app.UseAuthentication();

            return app;
        }
    }
}
