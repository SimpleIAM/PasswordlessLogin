// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services
{
    public class SendMessageResult
    {
        public bool MessageSent { get; set; }
        public string ErrorMessageForEndUser { get; set; }

        public static SendMessageResult Success()
        {
            return new SendMessageResult()
            {
                MessageSent = true,
            };
        }

        public static SendMessageResult Failed(string errorMessageForEndUser)
        {
            return new SendMessageResult()
            {
                MessageSent = false,
                ErrorMessageForEndUser = errorMessageForEndUser,
            };
        }
    }
}
