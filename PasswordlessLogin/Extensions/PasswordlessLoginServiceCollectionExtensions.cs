// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

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
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }


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

            return services;
        }

        private static void ThrowIfMissingParams(IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
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
        }

        private static IServiceCollection AddPasswordlessSignInPart1(this IServiceCollection services, IConfiguration configuration)
        {
            var connection = configuration.GetConnectionString(PasswordlessLoginConstants.ConfigurationSections.ConnectionStringName);

            services.AddDbContext<PasswordlessLoginDbContext>(options => options.UseSqlServer(connection));
            services.TryAddTransient<IOneTimeCodeStore, DbOneTimeCodeStore>();
            services.TryAddTransient<IOneTimeCodeService, OneTimeCodeService>();
            services.TryAddTransient<IUserStore, DbUserStore>();
            services.TryAddTransient<IAuthorizedDeviceStore, DbAuthorizedDeviceStore>();
            services.TryAddTransient<IMessageService, MessageService>();
            services.TryAddTransient<ISignInService, PasswordlessSignInService>();
            services.TryAddTransient<IApplicationService, NonexistantApplicationService>();

            return services;
        }

        private static IServiceCollection AddPasswordlessSignInPart2(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {
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
            return services;
        }

        public static IServiceCollection AddCustomCorsPolicy(this IServiceCollection services, string[] allowedOrigins)
        {
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

            return services;
        }
    }
}
