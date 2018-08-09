// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public enum AppType
    {
        ServerSideWebApp,
        ClientSideWebApp,
        NativeApp,
        BackendService
    }

    public class AppConfig
    {
        public AppType AppType { get; set; } = AppType.ServerSideWebApp;
        public string ClientId { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
        public string RedirectUris { get; set; }
        public string FrontChannelLogoutUri { get; set; }
        public string PostLogoutRedirectUris { get; set; }
        public string[] Secrets { get; set; }
        public string[] AllowedScopes { get; set; }
        public bool CallsAuthorityViaCors { get; set; }
        public IDictionary<string, string> CustomProperties { get; set; }
    }
}
