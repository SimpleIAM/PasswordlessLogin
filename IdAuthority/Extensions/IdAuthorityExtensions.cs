// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SimpleIAM.IdAuthority.Configuration;
using SimpleIAM.IdAuthority.Entities;
using SimpleIAM.IdAuthority.Services;
using SimpleIAM.IdAuthority.Services.Email;
using SimpleIAM.IdAuthority.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority
{
    public static class IdAuthorityExtensions
    {
        public static IServiceCollection AddIdAuthority(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var clients = configuration.GetSection("Apps").Get<List<ClientAppConfig>>() ?? new List<ClientAppConfig>();
            var idScopes = configuration.GetSection("IdScopes").Get<List<IdentityResource>>() ?? new List<IdentityResource>();
            idScopes.AddRange(new List<IdentityResource>() {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
            });

            var connection = configuration.GetConnectionString("IdAuthority");

            services.AddDbContext<IdAuthorityDbContext>(options => options.UseSqlServer(connection, b => b.MigrationsAssembly("IdAuthorityDemo")));
            services.AddTransient<IOneTimePasswordService, OneTimePasswordService>();
            services.AddTransient<ISubjectStore, DbSubjectStore>();

            services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = "/signin";
                options.UserInteraction.LogoutUrl = "/signout";
                options.UserInteraction.LogoutIdParameter = "id";
                options.UserInteraction.ErrorUrl = "/error";
            })
                .AddDeveloperSigningCredential() //todo: replace
                .AddInMemoryClients(ClientConfigHelper.GetClientsFromConfig(clients))
                .AddInMemoryIdentityResources(idScopes);

            var smtpConfig = new SmtpConfig();
            configuration.Bind("Mail:Smtp", smtpConfig);
            services.AddSingleton(smtpConfig);
            services.AddTransient<IEmailService, SmtpEmailService>();

            var emailTemplates = ProcessEmailTemplates.GetTemplatesFromMailConfig(configuration.GetSection("Mail"));
            services.AddSingleton(emailTemplates);
            services.AddTransient<IEmailTemplateService, EmailTemplateService>();

            services.AddEmbeddedViews();

            services.AddMvc();

            return services;
        }

        public static IApplicationBuilder UseIdAuthority(this IApplicationBuilder app, IHostingEnvironment env)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Security
            app.UseXContentTypeOptions();
            app.UseReferrerPolicy(opts => opts.SameOrigin());
            app.UseXXssProtection(options => options.EnabledWithBlockMode());
            app.UseXfo(options => options.Deny());

            app.UseCsp(opts => opts
                .BlockAllMixedContent()
                .DefaultSources(s => s.Self())
                .ScriptSources(s => {
                    s.Self();
                    s.CustomSources("sha256-VuNUSJ59bpCpw62HM2JG/hCyGiqoPN3NqGvNXQPU+rY=");
                })
                .StyleSources(s => {
                    s.Self();
                    s.UnsafeInline();
                })
                .FrameAncestors(s => s.None())
            );

            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new EmbeddedFileProvider(typeof(IdAuthorityExtensions).GetTypeInfo().Assembly, "SimpleIAM.IdAuthority.wwwroot")
            });

            app.UseIdentityServer();

            app.UseMvc();

            return app;
        }
    }
}
