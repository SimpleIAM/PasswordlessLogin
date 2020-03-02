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
using SimpleIAM.PasswordlessLogin.Helpers;
using SimpleIAM.PasswordlessLogin.Orchestrators;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Services.Email;
using SimpleIAM.PasswordlessLogin.Services.EventNotification;
using SimpleIAM.PasswordlessLogin.Services.Localization;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginBuilderExtensions
    {
        public static PasswordlessLoginBuilder AddPasswordlessLogin(this PasswordlessLoginBuilder builder)
        {
            var services = builder.Services;
            services.AddSingleton(builder.Options);

            services.TryAddTransient<IEventNotificationService, DefaultEventNotificationService>();
            services.TryAddTransient<IOneTimeCodeStore, DbOneTimeCodeStore>();
            services.TryAddTransient<IOneTimeCodeService, OneTimeCodeService>();
            services.TryAddTransient<IUserStore, DbUserStore>();
            services.TryAddTransient<ITrustedBrowserStore, DbTrustedBrowserStore>();
            services.TryAddTransient<IMessageService, MessageService>();
            services.TryAddTransient<ISignInService, PasswordlessSignInService>();
            services.TryAddTransient<IApplicationService, NonexistantApplicationService>();

            services.TryAddSingleton<IPasswordHashService>(
                new AspNetIdentityPasswordHashService(PasswordlessLoginConstants.Security.DefaultPbkdf2Iterations));
            services.TryAddTransient<IPasswordHashStore, DbPasswordHashStore>();
            services.TryAddTransient<IPasswordService, DefaultPasswordService>();
            services.TryAddTransient<IApplicationLocalizer, DefaultLocalizer>();

            services.TryAddTransient<AuthenticateOrchestrator>();
            services.TryAddTransient<UserOrchestrator>();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // IUrlHelper
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.TryAddScoped<IUrlHelper>(x =>
            {
                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });

            services.TryAddScoped<IUrlService, PasswordlessUrlService>();

            builder.AddEmailTemplates(TemplateFileProvider);
            builder.Services.TryAddTransient<IEmailTemplateService, EmailTemplateService>();

            return builder;
        }

        private static IFileProvider TemplateFileProvider =>
            new EmbeddedFileProvider(
                typeof(PasswordlessLoginServiceCollectionExtensions).GetTypeInfo().Assembly,
                $"SimpleIAM.PasswordlessLogin.{PasswordlessLoginConstants.EmailTemplateFolder}");

        public static PasswordlessLoginBuilder AddCustomEmailTemplates(this PasswordlessLoginBuilder builder, string templateOverrideFolder, CultureInfo[] supportedCultures = null)
        {
            if (!Directory.Exists(templateOverrideFolder ?? throw new ArgumentNullException(nameof(templateOverrideFolder))))
            {
                throw new Exception($"Custom email template folder '{templateOverrideFolder}' does not exist.");
            }

            IFileProvider templateFileProvider = new CompositeFileProvider(
                new PhysicalFileProvider(templateOverrideFolder),
                TemplateFileProvider
            );


            return builder.AddEmailTemplates(templateFileProvider, supportedCultures, true);
        }

        public static PasswordlessLoginBuilder AddSmtpEmail(this PasswordlessLoginBuilder builder, Action<SmtpOptions> smtpOptionsBuilder)
        {
            var smtpOptions = new SmtpOptions();
            smtpOptionsBuilder?.Invoke(smtpOptions);

            return builder.AddSmtpEmail(smtpOptions);
        }

        public static PasswordlessLoginBuilder AddSmtpEmail(this PasswordlessLoginBuilder builder, IConfiguration configuration)
        {
            var smtpOptions = new SmtpOptions();
            configuration.Bind(smtpOptions);

            return builder.AddSmtpEmail(smtpOptions);
        }

        public static PasswordlessLoginBuilder AddSmtpEmail(this PasswordlessLoginBuilder builder, SmtpOptions smtpOptions)
        {
            builder.Services.AddSingleton(smtpOptions);
            builder.Services.AddTransient<IEmailService, SmtpEmailService>();

            return builder;
        }

        public static PasswordlessLoginBuilder AddAuth(this PasswordlessLoginBuilder builder)
        { 
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(builder.Options.DefaultSessionLengthMinutes);
                    options.ConfigurePasswordlessAuthenticationOptions(builder.Options.Urls);
                });

            return builder;
        }

        public static void ConfigurePasswordlessAuthenticationOptions(this CookieAuthenticationOptions options, UrlOptions urls)
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
            if (urls.ApiBase != null)
            {
                options.Events.OnRedirectToLogin = options.Events.OnRedirectToLogin.ReturnStatusCode(HttpStatusCode.Unauthorized, urls.ApiBase);
            }
            if (urls.CustomApiBase != null)
            {
                options.Events.OnRedirectToLogin = options.Events.OnRedirectToLogin.ReturnStatusCode(HttpStatusCode.Unauthorized, urls.CustomApiBase);
            }
        }

        private static Func<RedirectContext<CookieAuthenticationOptions>, Task> ReturnStatusCode(this Func<RedirectContext<CookieAuthenticationOptions>, Task> existingRedirector, HttpStatusCode returnStatusCode, string forPath = null) => context =>
        {
            if (forPath == null || context.Request.Path.StartsWithSegments(forPath))
            {
                context.Response.StatusCode = (int)returnStatusCode;
                return Task.CompletedTask;
            }
            return existingRedirector(context);
        };

        private static PasswordlessLoginBuilder AddEmailTemplates(this PasswordlessLoginBuilder builder, IFileProvider templateFileProvider, CultureInfo[] supportedCultures = null, bool force = false)
        {
            var emailTemplates = EmailTemplateProcessor.GetTemplates(templateFileProvider, supportedCultures);
            if (force)
            {
                builder.Services.AddSingleton(emailTemplates);
            }
            else
            {
                builder.Services.TryAddSingleton(emailTemplates);
            }

            return builder;
        }
    }
}