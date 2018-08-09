// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class PasswordlessLoginApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePasswordlessSignIn(this IApplicationBuilder app, IFileProvider webRootFileProvider)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (webRootFileProvider == null)
            {
                throw new ArgumentNullException(nameof(webRootFileProvider));
            }

            var compositeFileProvider = new CompositeFileProvider(
                new EmbeddedFileProvider(typeof(PasswordlessLoginApplicationBuilderExtensions).GetTypeInfo().Assembly, "SimpleIAM.PasswordlessLogin.wwwroot"),
                webRootFileProvider
            );
            webRootFileProvider = compositeFileProvider;

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = compositeFileProvider
            });

            return app;
        }
    }
}
