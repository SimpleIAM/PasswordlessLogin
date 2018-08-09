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
using SimpleIAM.PasswordlessLogin;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Entities;
using SimpleIAM.PasswordlessLogin.Orchestrators;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Services.Email;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginServiceCollectionExtensions
    {
        public static IServiceCollection AddPasswordlessLogin(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var idProviderConfig = new IdProviderConfig();
            configuration.Bind(PasswordlessLoginConstants.ConfigurationSections.IdProvider, idProviderConfig);
            services.AddSingleton(idProviderConfig);

            var hostingConfig = new HostingConfig();
            configuration.Bind(PasswordlessLoginConstants.ConfigurationSections.Hosting, hostingConfig);
            services.AddSingleton(hostingConfig);

            var appConfigs = configuration.GetSection(PasswordlessLoginConstants.ConfigurationSections.Apps).Get<List<AppConfig>>() ?? new List<AppConfig>();
            var clients = AppConfigHelper.GetClientsFromAppConfig(appConfigs);
            var apps = AppConfigHelper.GetAppsFromClients(clients);
            var appStore = new InMemoryAppStore(apps);
            services.TryAddSingleton<IAppStore>(appStore);

            var idScopeConfig = configuration.GetSection(PasswordlessLoginConstants.ConfigurationSections.IdScopes).Get<List<IdScopeConfig>>() ?? new List<IdScopeConfig>();
            var idScopes = idScopeConfig.Select(x => new IdentityResource(x.Name, x.IncludeClaimTypes) { Required = x.Required }).ToList();
            idScopes.AddRange(new List<IdentityResource>() {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResources.Address(),
            });

            var apiConfigs = configuration.GetSection(PasswordlessLoginConstants.ConfigurationSections.Apis).Get<List<ApiConfig>>() ?? new List<ApiConfig>();
            var apiResources = apiConfigs.Select(x => new ApiResource(x.Url, x.IncludeClaimTypes)
            {
                ApiSecrets = x.Secrets?.ToList()?.Select(y=> new Secret(y.Sha256())).ToList(),
                Scopes = x.Scopes?.ToList()?.Select(y => new Scope(y)).ToList()
            }).ToList();

            var connection = configuration.GetConnectionString(PasswordlessLoginConstants.ConfigurationSections.ConnectionStringName);

            services.AddDbContext<PasswordlessLoginDbContext>(options => options.UseSqlServer(connection));
            services.TryAddTransient<IOneTimeCodeStore, DbOneTimeCodeStore>();
            services.TryAddTransient<IOneTimeCodeService, OneTimeCodeService>();
            services.TryAddTransient<IUserStore, DbUserStore>();
            services.TryAddTransient<IAuthorizedDeviceStore, DbAuthorizedDeviceStore>();
            services.TryAddTransient<IMessageService, MessageService>();

            services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = PasswordlessLoginConstants.Configuration.LoginUrl;
                options.UserInteraction.LogoutUrl = PasswordlessLoginConstants.Configuration.LogoutUrl;
                options.UserInteraction.LogoutIdParameter = PasswordlessLoginConstants.Configuration.LogoutIdParameter;
                options.UserInteraction.ErrorUrl = PasswordlessLoginConstants.Configuration.ErrorUrl;
                options.Authentication.CookieLifetime = TimeSpan.FromMinutes(idProviderConfig.DefaultSessionLengthMinutes);
            })
                .AddDeveloperSigningCredential() //todo: replace
                .AddInMemoryApiResources(apiResources)
                .AddInMemoryClients(clients)
                .AddProfileService<ProfileService>()                
                .AddInMemoryIdentityResources(idScopes);

            services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ReconfigureCookieOptions>();

            var smtpConfig = new SmtpConfig();
            configuration.Bind(PasswordlessLoginConstants.ConfigurationSections.Smtp, smtpConfig);
            services.TryAddSingleton(smtpConfig);
            services.TryAddTransient<IEmailService, SmtpEmailService>();

            IFileProvider templateFileProvider = new EmbeddedFileProvider(
                typeof(PasswordlessLoginServiceCollectionExtensions).GetTypeInfo().Assembly, 
                $"SimpleIAM.PasswordlessLogin.{PasswordlessLoginConstants.EmailTemplateFolder}");
            var emailTemplateOverrideFolder = Path.Combine(env.ContentRootPath, PasswordlessLoginConstants.EmailTemplateFolder);
            if (Directory.Exists(emailTemplateOverrideFolder))
            {
                templateFileProvider = new CompositeFileProvider(
                    new PhysicalFileProvider(emailTemplateOverrideFolder),
                    templateFileProvider
                );
            }
            var defaultFromAddress = configuration.GetValue<string>(PasswordlessLoginConstants.ConfigurationSections.MailFrom);
            var emailTemplates = EmailTemplateProcessor.GetTemplatesFromMailConfig(defaultFromAddress, templateFileProvider);
            services.TryAddSingleton(emailTemplates);
            services.TryAddTransient<IEmailTemplateService, EmailTemplateService>();

            services.TryAddSingleton<IPasswordHashService>(
                new AspNetIdentityPasswordHashService(PasswordlessLoginConstants.Security.DefaultPbkdf2Iterations));
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
                    options.AddPolicy(PasswordlessLoginConstants.Security.CorsPolicyName, builder => builder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(PasswordlessLoginConstants.Security.CorsPolicyName, builder => builder
                        .DisallowCredentials());
                });
            }

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.TryAddSingleton<ITempDataProvider, CookieTempDataProvider>();

            return services;
        }
    }
}
