// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;

namespace SimpleIAM.PasswordlessLogin
{
    public class Status
    {
        public IList<StatusMessage> Messages { get; set; } = new List<StatusMessage>();
        public string Text => string.Join("\n", Messages.Select(m => $"{m.Type.ToString()}: {m.Message}"));
        public bool HasError => Messages.Any(m => m.Type == StatusMessageType.Error);
        public bool IsOk => !HasError;

        [JsonIgnore]
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        [JsonIgnore]
        public string RedirectUrl { get; private set; } // only used with HttpStatusCode.Redirect

        public void Add(Status status)
        {
            Messages = Messages.Concat(status.Messages).ToList();
        }

        public void AddError(string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
        {
            Messages.Add(new StatusMessage { Type = StatusMessageType.Error, Message = message });
            StatusCode = httpStatusCode;
        }

        public void AddWarning(string message)
        {
            Messages.Add(new StatusMessage { Type = StatusMessageType.Warning, Message = message });
        }

        public void AddInfo(string message)
        {
            Messages.Add(new StatusMessage { Type = StatusMessageType.Info, Message = message });
        }

        public void AddSuccess(string message)
        {
            Messages.Add(new StatusMessage { Type = StatusMessageType.Success, Message = message });
        }

        public void AddSuccessIfNoMessages(string message)
        {
            if (!Messages.Any())
            {
                Messages.Add(new StatusMessage { Type = StatusMessageType.Success, Message = message });
            }
        }

        public static Status Error(string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
        {
            var status = new Status();
            status.AddError(message, httpStatusCode);
            return status;
        }

        public static Status Warning(string message)
        {
            var status = new Status();
            status.AddWarning(message);
            return status;
        }

        public static Status Info(string message)
        {
            var status = new Status();
            status.AddInfo(message);
            return status;
        }

        public static Status Success(string message = null)
        {
            var status = new Status();
            if (message != null)
            {
                status.AddSuccess(message);
            }
            return status;
        }

        public static Status Redirect(string redirectUrl, string message = null)
        {
            var status = new Status()
            {
                StatusCode = HttpStatusCode.Redirect,
                RedirectUrl = redirectUrl,
            };

            if (message != null)
            {
                status.AddSuccess(message);
            }
            return status;
        }
    }
}
