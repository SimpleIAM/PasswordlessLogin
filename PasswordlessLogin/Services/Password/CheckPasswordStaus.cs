using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public class CheckPasswordStatus : Status
    {
        public bool PasswordIncorrect { get; set; }
        public bool TemporarilyLocked { get; set; }
    }
}
