// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using StandardResponse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services.Message
{
    public interface IMessageService
    {
        Task<Status> SendAccountNotFoundMessageAsync(string applicationId, string sendTo);
        Task<Status> SendOneTimeCodeMessageAsync(string applicationId, string sendTo, string oneTimeCode);
        Task<Status> SendOneTimeCodeAndLinkMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode);
        Task<Status> SendWelcomeMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode, IDictionary<string, string> additionalMailMergeValues);
        Task<Status> SendPasswordResetMessageAsync(string applicationId, string sendTo, string oneTimeCode, string longCode);
        Task<Status> SendPasswordChangedNoticeAsync(string applicationId, string sendTo);
        Task<Status> SendPasswordRemovedNoticeAsync(string applicationId, string sendTo);
        Task<Status> SendEmailChangedNoticeAsync(string applicationId, string sendTo, string longCode);
    }
}
