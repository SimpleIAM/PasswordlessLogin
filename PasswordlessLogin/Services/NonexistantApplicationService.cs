using System.Collections.Generic;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public class NonexistantApplicationService : IApplicationService
    {
        public bool ApplicationExists(string applicationId)
        {
            return false;
        }

        public string GetApplicationName(string applicationId)
        {
            return null;
        }

        public IDictionary<string, string> GetApplicationProperties(string applicationId)
        {
            return new Dictionary<string, string>();
        }
    }
}
