// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class PasswordlessLoginAPIApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePasswordlessLoginAPI(this IApplicationBuilder app, IFileProvider webRootFileProvider)
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
                new EmbeddedFileProvider(typeof(PasswordlessLoginAPIApplicationBuilderExtensions).GetTypeInfo().Assembly, "SimpleIAM.PasswordlessLogin.API.wwwroot"),
                webRootFileProvider
            );

            #pragma warning disable IDE0059 // webRootFileProvider DOES need to be reasigned (?)
            webRootFileProvider = compositeFileProvider;
            #pragma warning restore IDE0059

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = compositeFileProvider
            });

            app.UseRouting(); // Must come after UseStaticFiles

            app.UseCors();

            return app;
        }
    }
}
