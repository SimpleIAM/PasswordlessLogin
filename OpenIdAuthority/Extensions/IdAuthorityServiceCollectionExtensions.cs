// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SimpleIAM.OpenIdAuthority;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Entities;
using SimpleIAM.OpenIdAuthority.Orchestrators;
using SimpleIAM.OpenIdAuthority.Services;
using SimpleIAM.OpenIdAuthority.Services.Email;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenIdAuthorityServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenIdAuthority(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var idProviderConfig = new IdProviderConfig();
            configuration.Bind(OpenIdAuthorityConstants.ConfigurationSections.IdProvider, idProviderConfig);
            services.AddSingleton(idProviderConfig);

            var hostingConfig = new HostingConfig();
            configuration.Bind(OpenIdAuthorityConstants.ConfigurationSections.Hosting, hostingConfig);
            services.AddSingleton(hostingConfig);

            var clientConfigs = configuration.GetSection(OpenIdAuthorityConstants.ConfigurationSections.Apps).Get<List<ClientAppConfig>>() ?? new List<ClientAppConfig>();
            var clients = ClientConfigHelper.GetClientsFromConfig(clientConfigs);
            var apps = ClientConfigHelper.GetAppsFromClients(clients);
            var appStore = new InMemoryAppStore(apps);
            services.TryAddSingleton<IAppStore>(appStore);

            var idScopeConfig = configuration.GetSection(OpenIdAuthorityConstants.ConfigurationSections.IdScopes).Get<List<IdScopeConfig>>() ?? new List<IdScopeConfig>();
            var idScopes = idScopeConfig.Select(x=> new IdentityResource(x.Name, x.DisplayName ?? x.Name, x.ClaimTypes) { Required = x.Required }).ToList();
            idScopes.AddRange(new List<IdentityResource>() {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResources.Address(),
            });

            var connection = configuration.GetConnectionString(OpenIdAuthorityConstants.ConfigurationSections.ConnectionStringName);

            services.AddDbContext<OpenIdAuthorityDbContext>(options => options.UseSqlServer(connection));
            services.TryAddTransient<IOneTimeCodeStore, DbOneTimeCodeStore>();
            services.TryAddTransient<IOneTimeCodeService, OneTimeCodeService>();
            services.TryAddTransient<IUserStore, DbUserStore>();
            services.TryAddTransient<IAuthorizedDeviceStore, DbAuthorizedDeviceStore>();
            services.TryAddTransient<IMessageService, MessageService>();

            services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = OpenIdAuthorityConstants.Configuration.LoginUrl;
                options.UserInteraction.LogoutUrl = OpenIdAuthorityConstants.Configuration.LogoutUrl;
                options.UserInteraction.LogoutIdParameter = OpenIdAuthorityConstants.Configuration.LogoutIdParameter;
                options.UserInteraction.ErrorUrl = OpenIdAuthorityConstants.Configuration.ErrorUrl;
                options.Authentication.CookieLifetime = TimeSpan.FromMinutes(idProviderConfig.DefaultSessionLengthMinutes);
            })
                .AddDeveloperSigningCredential() //todo: replace
                .AddInMemoryClients(clients)
                .AddProfileService<ProfileService>()                
                .AddInMemoryIdentityResources(idScopes);

            services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ReconfigureCookieOptions>();

            var smtpConfig = new SmtpConfig();
            configuration.Bind(OpenIdAuthorityConstants.ConfigurationSections.Smtp, smtpConfig);
            services.TryAddSingleton(smtpConfig);
            services.TryAddTransient<IEmailService, SmtpEmailService>();

            IFileProvider templateFileProvider = new EmbeddedFileProvider(
                typeof(OpenIdAuthorityServiceCollectionExtensions).GetTypeInfo().Assembly, 
                $"SimpleIAM.OpenIdAuthority.{OpenIdAuthorityConstants.EmailTemplateFolder}");
            var emailTemplateOverrideFolder = Path.Combine(env.ContentRootPath, OpenIdAuthorityConstants.EmailTemplateFolder);
            if (Directory.Exists(emailTemplateOverrideFolder))
            {
                templateFileProvider = new CompositeFileProvider(
                    new PhysicalFileProvider(emailTemplateOverrideFolder),
                    templateFileProvider
                );
            }
            var defaultFromAddress = configuration.GetValue<string>(OpenIdAuthorityConstants.ConfigurationSections.MailFrom);
            var emailTemplates = EmailTemplateProcessor.GetTemplatesFromMailConfig(defaultFromAddress, templateFileProvider);
            services.TryAddSingleton(emailTemplates);
            services.TryAddTransient<IEmailTemplateService, EmailTemplateService>();

            services.TryAddSingleton<IPasswordHashService>(
                new AspNetIdentityPasswordHashService(OpenIdAuthorityConstants.Security.DefaultPbkdf2Iterations));
            services.TryAddTransient<IPasswordHashStore, DbPasswordHashStore>();
            services.TryAddTransient<IPasswordService, DefaultPasswordService>();

            services.TryAddTransient<AuthenticateOrchestrator>();
            services.TryAddTransient<UserOrchestrator>();

            services.AddEmbeddedViews();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // IUrlHelper
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.TryAddScoped<IUrlHelper>(x => {
                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });

            var allowedOrigins = clients.SelectMany(x => x.AllowedCorsOrigins).Distinct().ToArray();
            if (allowedOrigins.Length > 0)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(OpenIdAuthorityConstants.Security.CorsPolicyName, builder => builder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
                });
            }

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.TryAddSingleton<ITempDataProvider, CookieTempDataProvider>();

            return services;
        }
    }
}
