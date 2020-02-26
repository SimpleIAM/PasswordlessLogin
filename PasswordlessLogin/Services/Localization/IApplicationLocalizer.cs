using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services.Localization
{
    // Simple localized interface that can be implemented with a StringLocalizer if localization is required
    public interface IApplicationLocalizer
    {
        LocalizedString this[string name] { get; }
        LocalizedString this[string name, params object[] arguments] { get; }
    }
}
