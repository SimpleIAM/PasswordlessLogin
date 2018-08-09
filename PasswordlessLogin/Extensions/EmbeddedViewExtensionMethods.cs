// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace SimpleIAM.PasswordlessLogin
{
    public static class EmbeddedViewExtensionMethods
    {
        /// <summary>
        /// Adds ability to use views embedded in packages. Add before calling services.UseMvc().
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEmbeddedViews(this IServiceCollection services)
        {
            var assembly = typeof(EmbeddedViewExtensionMethods).GetTypeInfo().Assembly;

            var embeddedFileProvider = new EmbeddedFileProvider(assembly, "SimpleIAM.PasswordlessLogin.UI");

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Add(embeddedFileProvider);
                options.ViewLocationExpanders.Add(new EmbeddedViewLocator());
            }
            );
            return services;
        }
    }
}
