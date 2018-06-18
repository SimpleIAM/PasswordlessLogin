// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using SimpleIAM.OpenIdAuthority.Configuration;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{
    public static class OpenIdAuthorityApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOpenIdAuthority(this IApplicationBuilder app, IHostingEnvironment env, HostingConfig hostingConfig)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            hostingConfig = hostingConfig ?? new HostingConfig();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();              
            }

            if(hostingConfig.BehindProxy)
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
            }

            if (!hostingConfig.SkipContentSecuritySetup)
            {
                // Security
                app.UseXContentTypeOptions();
                app.UseReferrerPolicy(opts => opts.SameOrigin());
                app.UseXXssProtection(options => options.EnabledWithBlockMode());
                app.UseXfo(options => options.Deny());

                app.UseCsp(opts => opts
                    .BlockAllMixedContent()
                    .DefaultSources(s => s.Self())
                    .ScriptSources(s =>
                    {
                        s.Self();
                        var scriptSrcs = (hostingConfig.Csp.ScriptSources ?? new string[] { }).ToList();
                        scriptSrcs.Add("sha256-VuNUSJ59bpCpw62HM2JG/hCyGiqoPN3NqGvNXQPU+rY=");
                        s.CustomSources(scriptSrcs.ToArray());
                    })
                    .StyleSources(s =>
                    {
                        s.Self();
                        s.UnsafeInline();
                        if (hostingConfig.Csp.StyleSources != null)
                        {
                            s.CustomSources(hostingConfig.Csp.StyleSources);
                        }
                    })
                    .FontSources(s =>
                    {
                        s.Self();
                        if (hostingConfig.Csp.FontSources != null)
                        {
                            s.CustomSources(hostingConfig.Csp.FontSources);
                        }
                    })
                    .ImageSources(s =>
                    {
                        s.Self();
                        if (hostingConfig.Csp.ImageSources != null)
                        {
                            s.CustomSources(hostingConfig.Csp.ImageSources);
                        }
                    })
                    .FrameAncestors(s => s.None())
                );
                app.UseMiddleware<SimpleIAM.OpenIdAuthority.Extensions.CspHeaderOverridesMiddleware>();
            }

            var compositeFileProvider = new CompositeFileProvider(
                new EmbeddedFileProvider(typeof(OpenIdAuthorityApplicationBuilderExtensions).GetTypeInfo().Assembly, "SimpleIAM.OpenIdAuthority.wwwroot"),
                env.WebRootFileProvider
            );
            env.WebRootFileProvider = compositeFileProvider;

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = compositeFileProvider
            });

            app.UseIdentityServer();

            app.UseMvc();

            return app;
        }
    }
}
