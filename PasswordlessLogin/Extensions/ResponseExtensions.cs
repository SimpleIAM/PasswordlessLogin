using Microsoft.AspNetCore.Mvc;
using StandardResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleIAM.PasswordlessLogin.Extensions
{
    public static class ResponseExtensions
    {
        public static JsonResult ToJsonResult(this WebStatus status)
        {
            return new JsonResult(new { 
                Message = status.Text, 
                Messages = status.Messages,
                HasError = status.HasError,
                IsOK = status.IsOk
            }) { 
                StatusCode = (int)status.StatusCode 
            };
        }

        public static JsonResult ToJsonResult<T>(this Response<T, WebStatus> response)
        {
            if(response.HasError)
            {
                return response.Status.ToJsonResult();
            }
            return new JsonResult(response.Result) { StatusCode = (int)response.Status.StatusCode };
        }
    }
}
