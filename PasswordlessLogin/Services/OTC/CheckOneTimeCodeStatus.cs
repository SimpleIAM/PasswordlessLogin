// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public class CheckOneTimeCodeStatus : Status
    {
        public CheckOneTimeCodeStatusCode StatusCode { get; set; }

        public static CheckOneTimeCodeStatus Error(string message, CheckOneTimeCodeStatusCode statusCode)
        {
            var status = Error<CheckOneTimeCodeStatus>(message);
            status.StatusCode = statusCode;
            return status;
        }

        public new static CheckOneTimeCodeStatus Success(string message, CheckOneTimeCodeStatusCode statusCode)
        {
            var status = Success<CheckOneTimeCodeStatus>(message);
            status.StatusCode = statusCode;
            return status;
        }
    }
}
