// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.PasswordlessLogin.Services.EventNotification
{
    public enum EventType
    {
        Register,
        RequestOneTimeCode,
        RequestPasswordReset,
        SignInSuccess,
        SignInFail,
        SetPassword,
        RemovePassword,
        UpdateAccount,
        EmailChange,
        CancelEmailChange,
        AccountNotFound
    }
}
