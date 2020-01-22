﻿using StandardResponse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Services.Password
{
    public class CheckPasswordStatus : Status
    {
        public CheckPasswordStatusCode StatusCode { get; set; } = CheckPasswordStatusCode.Success;

        public static CheckPasswordStatus Error(string message, CheckPasswordStatusCode statusCode)
        {
            var status = Error<CheckPasswordStatus>(message);
            status.StatusCode = statusCode;
            return status;
        }
    }
}
