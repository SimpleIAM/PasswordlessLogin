// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    public enum AppType
    {
        Traditional,
        SPA
    }

    public class ClientAppConfig
    {
        public AppType AppType { get; set; } = AppType.Traditional;
        public string ClientId { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
        public string RedirectUris { get; set; }
        public string FrontChannelLogoutUri { get; set; }
        public string PostLogoutRedirectUris { get; set; }
        public string ClientSecret { get; set; }
    }
}
