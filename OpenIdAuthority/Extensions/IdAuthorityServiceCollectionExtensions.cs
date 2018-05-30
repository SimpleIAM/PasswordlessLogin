// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SimpleIAM.OpenIdAuthority;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Entities;
using SimpleIAM.OpenIdAuthority.Orchestrators;
using SimpleIAM.OpenIdAuthority.Services.Email;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenIdAuthorityServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenIdAuthority(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var idProviderConfig = new IdProviderConfig();
            configuration.Bind("IdProvider", idProviderConfig);
            services.AddSingleton(idProviderConfig);

            var hostingConfig = new HostingConfig();
            configuration.Bind("Hosting", hostingConfig);
            services.AddSingleton(hostingConfig);

            var clientConfigs = configuration.GetSection("Apps").Get<List<ClientAppConfig>>() ?? new List<ClientAppConfig>();
            var clients = ClientConfigHelper.GetClientsFromConfig(clientConfigs);
            var apps = ClientConfigHelper.GetAppsFromClients(clients);
            var appStore = new InMemoryAppStore(apps);
            services.AddSingleton<IAppStore>(appStore);

            var idScopes = configuration.GetSection("IdScopes").Get<List<IdentityResource>>() ?? new List<IdentityResource>();
            idScopes.AddRange(new List<IdentityResource>() {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
            });

            var connection = configuration.GetConnectionString("OpenIdAuthority");

            services.AddDbContext<OpenIdAuthorityDbContext>(options => options.UseSqlServer(connection));
            services.AddTransient<IOneTimeCodeStore, DbOneTimeCodeStore>();
            services.AddTransient<IOneTimeCodeService, OneTimeCodeService>();
            services.AddTransient<ISubjectStore, DbSubjectStore>();
            services.AddTransient<IMessageService, MessageService>();

            services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = "/signin";
                options.UserInteraction.LogoutUrl = "/signout";
                options.UserInteraction.LogoutIdParameter = "id";
                options.UserInteraction.ErrorUrl = "/error";
                options.Authentication.CookieLifetime = TimeSpan.FromMinutes(idProviderConfig.DefaultSessionLengthMinutes);
            })
                .AddDeveloperSigningCredential() //todo: replace
                .AddInMemoryClients(clients)
                .AddInMemoryIdentityResources(idScopes);

            var smtpConfig = new SmtpConfig();
            configuration.Bind("Mail:Smtp", smtpConfig);
            services.AddSingleton(smtpConfig);
            services.AddTransient<IEmailService, SmtpEmailService>();

            var emailTemplates = ProcessEmailTemplates.GetTemplatesFromMailConfig(configuration.GetSection("Mail"));
            services.AddSingleton(emailTemplates);
            services.AddTransient<IEmailTemplateService, EmailTemplateService>();

            services.AddSingleton<IPasswordHashService>(new AspNetIdentityPasswordHashService(10000));
            services.AddTransient<IPasswordHashStore, DbPasswordHashStore>();
            services.AddTransient<IPasswordService, DefaultPasswordService>();

            services.AddTransient<AuthenticateOrchestrator>();            

            services.AddEmbeddedViews();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // IUrlHelper
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IUrlHelper>(x => {
                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });

            var allowedOrigins = hostingConfig.CorsOrigins ?? new string[] { };
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder => builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddMvc();

            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();

            return services;
        }
    }
}
