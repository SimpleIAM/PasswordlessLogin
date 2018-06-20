// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SimpleIAM.OpenIdAuthority.Orchestrators
{
    public class ActionResponse
    {
        public ActionResponse() { }

        public ActionResponse(string message = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Message = message;
            StatusCode = statusCode;
        }

        public ActionResponse(object content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Content = content;
            StatusCode = statusCode;
        }

        public ActionResponse(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public ActionResponse(ModelStateDictionary modelState, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            StatusCode = statusCode;
            // Extract error messages from model state. For form-level errors, use the key "_" instead of ""
            Errors = modelState
                .ToDictionary(x => x.Key, x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                .Where(em => em.Value.Count() > 0)
                .ToDictionary(x => x.Key == "" ? "_" : x.Key, x => x.Value);

            var kvp = Errors.FirstOrDefault();
            Message = kvp.Value.FirstOrDefault();
            if (Message != null && kvp.Key != null && kvp.Key != "_")
            {
                Message = $"{kvp.Key}: {Message}";
            }
        }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public string Message { get; set; }

        public object Content { get; set; }

        public Dictionary<string, string[]> Errors { get; set; }

        public string RedirectUrl { get; set; }

        public JsonResult ToJsonResult()
        {
            if(Content != null)
            {
                return new JsonResult(Content) { StatusCode = (int)StatusCode };
            }
            return new JsonResult(new { Message, Errors }) { StatusCode = (int)StatusCode };
        }
    }
}
