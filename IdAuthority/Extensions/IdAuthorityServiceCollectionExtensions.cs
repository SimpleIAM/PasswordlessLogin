// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SimpleIAM.IdAuthority;
using SimpleIAM.IdAuthority.Configuration;
using SimpleIAM.IdAuthority.Entities;
using SimpleIAM.IdAuthority.Services;
using SimpleIAM.IdAuthority.Services.Email;
using SimpleIAM.IdAuthority.Stores;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdAuthorityServiceCollectionExtensions
    {
        public static IServiceCollection AddIdAuthority(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var idProviderConfig = new IdProviderConfig();
            configuration.Bind("IdProvider", idProviderConfig);
            services.AddSingleton(idProviderConfig);

            var clients = configuration.GetSection("Apps").Get<List<ClientAppConfig>>() ?? new List<ClientAppConfig>();
            var idScopes = configuration.GetSection("IdScopes").Get<List<IdentityResource>>() ?? new List<IdentityResource>();
            idScopes.AddRange(new List<IdentityResource>() {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
            });

            var connection = configuration.GetConnectionString("IdAuthority");

            services.AddDbContext<IdAuthorityDbContext>(options => options.UseSqlServer(connection));
            services.AddTransient<IOneTimePasswordService, OneTimePasswordService>();
            services.AddTransient<ISubjectStore, DbSubjectStore>();

            services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = idProviderConfig.DefaultSignInMethod == SignInMethod.Email ? "/signin" : "/signinpass";
                options.UserInteraction.LogoutUrl = "/signout";
                options.UserInteraction.LogoutIdParameter = "id";
                options.UserInteraction.ErrorUrl = "/error";
                options.Authentication.CookieLifetime = TimeSpan.FromMinutes(idProviderConfig.DefaultSessionLengthMinutes);
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

            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();

            return services;
        }
    }
}
