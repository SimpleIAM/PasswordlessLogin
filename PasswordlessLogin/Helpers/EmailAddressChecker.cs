using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Helpers
{
    public static class EmailAddressChecker
    {
        public static bool EmailIsValid(string email)
        {
            // TODO: implement better check
            if (email == null || email.Length > 254)
            {
                return false;
            }
            return email.IndexOf("@") > 0;
        }
    }
}
