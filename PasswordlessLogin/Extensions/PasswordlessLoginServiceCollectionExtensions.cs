// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
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
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginServiceCollectionExtensions
    {
        public static IServiceCollection AddPasswordlessLogin(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env, string[] apiAllowedOrigins = null)
        {
            services.AddPasswordlessLoginWithoutAuthentication(configuration, env, apiAllowedOrigins);

            var idProviderConfig = new IdProviderConfig();
            configuration.Bind(PasswordlessLoginConstants.ConfigurationSections.IdProvider, idProviderConfig);

            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(idProviderConfig.DefaultSessionLengthMinutes);
                    options.ConfigurePasswordlessAuthenticationOptions(idProviderConfig.Urls);
                });

            return services;
        }

        public static void ConfigurePasswordlessAuthenticationOptions(this CookieAuthenticationOptions options, UrlConfig urls)
        {
            options.LoginPath = urls.SignIn;
            options.LogoutPath = urls.SignOut;
            options.SlidingExpiration = true;
            options.ReturnUrlParameter = "returnUrl";

            options.Cookie.IsEssential = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Events.OnRedirectToAccessDenied = context =>
            {
                // Don't redirect to another page
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return Task.FromResult(0);
            };
            options.Events.OnRedirectToLogin = options.Events.OnRedirectToLogin.ReturnStatusCode(HttpStatusCode.Unauthorized, urls.ApiBase);
            if (urls.CustomApiBase != null)
            {
                options.Events.OnRedirectToLogin = options.Events.OnRedirectToLogin.ReturnStatusCode(HttpStatusCode.Unauthorized, urls.CustomApiBase);
            }
        }

        public static Func<RedirectContext<CookieAuthenticationOptions>, Task> ReturnStatusCode(this Func<RedirectContext<CookieAuthenticationOptions>, Task> existingRedirector, HttpStatusCode returnStatusCode, string forPath = null) => context =>
        {
            if (forPath == null || context.Request.Path.StartsWithSegments(forPath))
            {
                context.Response.StatusCode = (int)returnStatusCode;
                return Task.CompletedTask;
            }
            return existingRedirector(context);
        };

        public static IServiceCollection AddPasswordlessLoginWithoutAuthentication(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env, string[] apiAllowedOrigins = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            var idProviderConfig = new IdProviderConfig();
            configuration.Bind(PasswordlessLoginConstants.ConfigurationSections.IdProvider, idProviderConfig);
            services.AddSingleton(idProviderConfig);

            var databaseConfig = new PasswordlessDatabaseConfig();
            configuration.Bind(PasswordlessLoginConstants.ConfigurationSections.PasswordlessDatabase, databaseConfig);
            services.AddSingleton(databaseConfig);

            var connection = configuration.GetConnectionString(PasswordlessLoginConstants.ConfigurationSections.ConnectionStringName);

            services.AddDbContext<PasswordlessLoginDbContext>(options => options.UseSqlServer(connection));
            services.TryAddTransient<IOneTimeCodeStore, DbOneTimeCodeStore>();
            services.TryAddTransient<IOneTimeCodeService, OneTimeCodeService>();
            services.TryAddTransient<IUserStore, DbUserStore>();
            services.TryAddTransient<IAuthorizedDeviceStore, DbAuthorizedDeviceStore>();
            services.TryAddTransient<IMessageService, MessageService>();
            services.TryAddTransient<ISignInService, PasswordlessSignInService>();
            services.TryAddTransient<IApplicationService, NonexistantApplicationService>();

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
            var defaultFromAddress = configuration.GetValue<string>(PasswordlessLoginConstants.ConfigurationSections.MailFrom) ?? "from.address@notconfigured.yet";
            var emailTemplates = EmailTemplateProcessor.GetTemplatesFromMailConfig(defaultFromAddress, templateFileProvider);
            services.TryAddSingleton(emailTemplates);
            services.TryAddTransient<IEmailTemplateService, EmailTemplateService>();

            services.TryAddSingleton<IPasswordHashService>(
                new AspNetIdentityPasswordHashService(PasswordlessLoginConstants.Security.DefaultPbkdf2Iterations));
            services.TryAddTransient<IPasswordHashStore, DbPasswordHashStore>();
            services.TryAddTransient<IPasswordService, DefaultPasswordService>();

            services.TryAddTransient<AuthenticateOrchestrator>();
            services.TryAddTransient<UserOrchestrator>();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // IUrlHelper
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.TryAddScoped<IUrlHelper>(x => {
                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });

            services.TryAddScoped<IUrlService, PasswordlessUrlService>();

            services.AddPasswordlessCorsPolicy(apiAllowedOrigins);

            return services;
        }

        private static IServiceCollection AddPasswordlessCorsPolicy(this IServiceCollection services, string[] allowedOrigins = null)
        {
            if (allowedOrigins?.Length > 0)
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

            return services;
        }
    }
}
