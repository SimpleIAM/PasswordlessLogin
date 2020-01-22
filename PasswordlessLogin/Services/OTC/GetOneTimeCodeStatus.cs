// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.PasswordlessLogin.Models;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Services.OTC
{
    public class GetOneTimeCodeStatus : Status
    {
        public GetOneTimeCodeStatusCode StatusCode { get; set; } = GetOneTimeCodeStatusCode.Success;

        public static GetOneTimeCodeStatus Error(string message, GetOneTimeCodeStatusCode statusCode)
        {
            var status = Error<GetOneTimeCodeStatus>(message);
            status.StatusCode = statusCode;
            return status;
        }

        public new static GetOneTimeCodeStatus Success(string message)
        {
            return Success<GetOneTimeCodeStatus>(message);
        }
    }
}
