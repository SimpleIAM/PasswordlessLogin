// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using SimpleIAM.OpenIdAuthority.Models;
using System.Collections.Generic;
using System.Linq;

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public static class ClientConfigHelper
    {
        public static IEnumerable<Client> GetClientsFromConfig(IEnumerable<ClientAppConfig> config)
        {
            return config.Select(x => GetClientFromConfig(x));
        }

        public static Client GetClientFromConfig(ClientAppConfig config)
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
                ClientSecrets = new Secret[] { new Secret(config.ClientSecret.Sha256()) },
                AllowedGrantTypes = config.AppType == AppType.SPA ? new string[] { "implicit" } : new string[] { "authorization_code" },
                RedirectUris = config.RedirectUris != null ? config.RedirectUris.Split('\n') : new string[] { baseUrl, $"{baseUrl}/signin-oidc" },
                FrontChannelLogoutUri = config.FrontChannelLogoutUri ?? $"{baseUrl}/signout-oidc",
                PostLogoutRedirectUris = config.PostLogoutRedirectUris != null ? config.PostLogoutRedirectUris.Split('\n') : new string[] { baseUrl, $"{baseUrl}/", $"{baseUrl}/signout-callback-oidc" },
                AllowedScopes = allowedScopes.Distinct().ToArray(),
                RequireConsent = false,
                Properties = config.CustomProperties ?? new Dictionary<string, string>(),
            };
            if(config.AppType == AppType.SPA)
            {
                client.AllowedCorsOrigins.Add(baseUrl); //todo: consider enabling cors for server side apps too
                client.AllowAccessTokensViaBrowser = true;
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
