// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Net;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public abstract class ActionResponder
    {
        protected Status Ok(string message = null)
        {
            return Status.Success(message);
        }
        protected Status Redirect(string url)
        {
            return Status.Redirect(url);
        }
        protected Status BadRequest(string message = null)
        {
            return Status.Error(message, HttpStatusCode.BadRequest);
        }
        protected Status Unauthenticated(string message = null)
        {
            return Status.Error(message, HttpStatusCode.Unauthorized);
        }
        protected Status PermissionDenied(string message = null)
        {
            return Status.Error(message, HttpStatusCode.Forbidden);
        }
        protected Status NotFound(string message = null)
        {
            return Status.Error(message, HttpStatusCode.NotFound);
        }
        protected Status Conflict(string message = null)
        {
            return Status.Error(message, HttpStatusCode.Conflict);
        }
        protected Status ServerError(string message = null)
        {
            return Status.Error(message, HttpStatusCode.InternalServerError);
        }
    }
}
