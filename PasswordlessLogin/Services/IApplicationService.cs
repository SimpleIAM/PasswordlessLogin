using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public interface IApplicationService
    {
        bool ApplicationExists(string applicationId);
        string GetApplicationName(string applicationId);
        IDictionary<string, string> GetApplicationProperties(string applicationId);
    }
}
