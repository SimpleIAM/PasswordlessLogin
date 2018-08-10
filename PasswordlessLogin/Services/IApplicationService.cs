// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public interface IApplicationService
    {
        bool ApplicationExists(string applicationId);
        string GetApplicationName(string applicationId);
        IDictionary<string, string> GetApplicationProperties(string applicationId);
    }
}
