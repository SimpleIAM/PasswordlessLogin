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
        public static PasswordlessLoginBuilder AddPasswordlessLogin(this IServiceCollection services, Action<PasswordlessLoginOptions> optionsAction = null)
        {
            var passwordlessLoginOptions = new PasswordlessLoginOptions();
            optionsAction?.Invoke(passwordlessLoginOptions);

            var builder = new PasswordlessLoginBuilder(services, passwordlessLoginOptions);
            return builder.AddPasswordlessLogin();
        }

        public static PasswordlessLoginBuilder AddPasswordlessLogin(this IServiceCollection services, IConfiguration configuration)
        {
            var passwordlessLoginOptions = new PasswordlessLoginOptions();
            configuration.Bind(passwordlessLoginOptions);
            var builder = new PasswordlessLoginBuilder(services, passwordlessLoginOptions);
            return builder.AddPasswordlessLogin();
        }
    }
}
