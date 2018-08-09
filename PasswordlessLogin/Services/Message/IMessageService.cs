// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Message
{
    public interface IMessageService
    {
        Task<SendMessageResult> SendAccountNotFoundMessageAsync(string applicationId, string sendTo);
        Task<SendMessageResult> SendOneTimeCodeMessageAsync(string applicationId, string sendTo, string oneTimeCode);
        Task<SendMessageResult> SendOneTimeCodeAndLinkMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode);
        Task<SendMessageResult> SendWelcomeMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode, IDictionary<string, string> additionalMailMergeValues);
        Task<SendMessageResult> SendPasswordResetMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode);
    }
}
