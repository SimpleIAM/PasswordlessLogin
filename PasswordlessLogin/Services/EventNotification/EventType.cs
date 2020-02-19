// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services.EventNotification
{
    public static class EventType
    {
        public static string Register = "Register";
        public static string RequestOneTimeCode = "RequestOneTimeCode";
        public static string RequestPasswordReset = "RequestPasswordReset";
        public static string SignInSuccess = "SignInSuccess";
        public static string SignInFail = "SignInFail";
        public static string SetPassword = "SetPassword";
        public static string RemovePassword = "RemovePassword";
        public static string UpdateAccount = "UpdateAccount";
        public static string EmailChange = "EmailChange";
        public static string CancelEmailChange = "CancelEmailChange";
        public static string AccountNotFound = "AccountNotFound";
    }
}
