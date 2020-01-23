// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using StandardResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Extensions
{
    public static class ModelStateExtensions
    {
        public static WebStatus GetStatus(this ModelStateDictionary modelState, HttpStatusCode errorStatusCode = HttpStatusCode.BadRequest)
        {
            var status = new WebStatus();
            foreach (var error in modelState.Values.SelectMany(modelStateEntry => modelStateEntry.Errors))
            {
                status.AddError(error.ErrorMessage ?? "Internal Server Error"); // if an exception, don't leak the potentially sensitive details
            }

            if (status.HasError)
            {
                status.StatusCode = errorStatusCode;
            }
            return status;
        }
    }
}
