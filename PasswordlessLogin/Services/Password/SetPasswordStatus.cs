using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public class SetPasswordStatus : Status
    {
        public bool PasswordDoesNotMeetStrengthRequirements { get; set; }
    }
}
