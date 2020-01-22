// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using StandardResponse;
using System.Net;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public abstract class ActionResponder
    {
        protected WebStatus Ok(string message = null)
        {
            return WebStatus.Success(message);
        }

        protected WebStatus Redirect(string url)
        {
            return WebStatus.Redirect(url);
        }

        protected WebStatus BadRequest(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.BadRequest);
        }
        
        protected WebStatus Unauthenticated(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.Unauthorized);
        }
        
        protected Status PermissionDenied(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.Forbidden);
        }
        
        protected WebStatus NotFound(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.NotFound);
        }
        
        protected WebStatus Conflict(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.Conflict);
        }
        
        protected WebStatus ServerError(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.InternalServerError);
        }
    }
}
