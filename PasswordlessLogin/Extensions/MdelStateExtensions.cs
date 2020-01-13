using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Extensions
{
    public static class MdelStateExtensions
    {
        public static Status GetStatus(this ModelStateDictionary modelState, HttpStatusCode errorStatusCode = HttpStatusCode.BadRequest)
        {
            var status = new Status();
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
