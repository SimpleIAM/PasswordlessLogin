// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleIAM.IdAuthority.Configuration
{
    public static class ClientConfigHelper
    {
        public static IEnumerable<Client> GetClientsFromConfig(IEnumerable<ClientAppConfig> config)
        {
            return config.Select(x => GetClientFromConfig(x));
        }

        public static Client GetClientFromConfig(ClientAppConfig config)
        {
            var baseUrl = (config.Uri ?? "").TrimEnd('/');

            var client = new Client()
            {
                ClientId = config.ClientId,
                ClientSecrets = new Secret[] { new Secret(config.ClientSecret.Sha256()) },
                AllowedGrantTypes = config.AppType == AppType.SPA ? new string[] { "implicit" } : new string[] { "authorization_code" },
                RedirectUris = config.RedirectUris != null ? config.RedirectUris.Split('\n') : new string[] { $"{baseUrl}/signin-oidc" },
                FrontChannelLogoutUri = config.FrontChannelLogoutUri ?? $"{baseUrl}/signout-oidc",
                PostLogoutRedirectUris = config.PostLogoutRedirectUris != null ? config.PostLogoutRedirectUris.Split('\n') : new string[] { $"{baseUrl}/signout-callback-oidc" },
                AllowedScopes = new string[] { "openid", "profile" },
                RequireConsent = false
            };

            return client;
        }
    }
}
