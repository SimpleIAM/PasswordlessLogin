using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services.Localization
{
    public class DefaultLocalizer : IApplicationLocalizer
    {
        public LocalizedString this[string name]
        {
            get
            {
                return this[name, new object[] { }];
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                return new LocalizedString(name, string.Format(name, arguments));
            }
        }
    }
}
