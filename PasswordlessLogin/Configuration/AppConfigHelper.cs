﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using SimpleIAM.PasswordlessLogin.Models;
using System.Collections.Generic;
using System.Linq;

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public static class AppConfigHelper
    {
        public static IEnumerable<Client> GetClientsFromAppConfig(IEnumerable<AppConfig> config)
        {
            return config.Select(x => GetClientFromAppConfig(x));
        }

        public static Client GetClientFromAppConfig(AppConfig config)
        {
            //todo: clean up magic strings
            var baseUrl = (config.Uri ?? "").TrimEnd('/');
            var allowedScopes = new List<string>() { "openid" };
            if (config.AllowedScopes != null)
            {
                allowedScopes.AddRange(config.AllowedScopes);
            }

            var client = new Client()
            {
                ClientId = config.ClientId,
                ClientName = config.Name,
                ClientUri = baseUrl,
                ClientSecrets = config.Secrets?.Select(x => new Secret(x.Sha256())).ToList() ?? new List<Secret>(),
                AllowedGrantTypes = config.AppType == AppType.ClientSideWebApp ? new string[] { "implicit" } : new string[] { "authorization_code" },
                RedirectUris = config.RedirectUris != null ? config.RedirectUris.Split('\n') : new string[] { baseUrl, $"{baseUrl}/signin-oidc" },
                FrontChannelLogoutUri = config.FrontChannelLogoutUri ?? $"{baseUrl}/signout-oidc",
                PostLogoutRedirectUris = config.PostLogoutRedirectUris != null ? config.PostLogoutRedirectUris.Split('\n') : new string[] { baseUrl, $"{baseUrl}/", $"{baseUrl}/signout-callback-oidc" },
                AllowedScopes = allowedScopes.Distinct().ToArray(),
                RequireConsent = false,
                Properties = config.CustomProperties ?? new Dictionary<string, string>(),
            };
            if (config.CallsAuthorityViaCors)
            {
                client.AllowedCorsOrigins.Add(baseUrl);
            }
            switch(config.AppType)
            {
                case AppType.ClientSideWebApp:
                    client.AllowedGrantTypes = new string[] { "implicit" };
                    client.AllowAccessTokensViaBrowser = true;
                    break;
                case AppType.NativeApp:
                    client.AllowedGrantTypes = new string[] { "implicit" };
                    client.AllowAccessTokensViaBrowser = true;
                    break;
                case AppType.BackendService:
                    client.AllowedGrantTypes = new string[] { "client_credentials" };
                    break;
                case AppType.ServerSideWebApp:
                default:
                    client.AllowedGrantTypes = new string[] { "authorization_code" };
                    break;
            }

            return client;
        }

        public static IEnumerable<App> GetAppsFromClients(IEnumerable<Client> clients)
        {
            return clients.Select(x => GetAppFromClient(x));
        }

        public static App GetAppFromClient(Client client)
        {
            var app = new App()
            {
                Name = client.ClientName,
                Uri = client.ClientUri,
                LogoUri = client.LogoUri ?? "/applogo.png",
            };

            return app;
        }
    }
}
