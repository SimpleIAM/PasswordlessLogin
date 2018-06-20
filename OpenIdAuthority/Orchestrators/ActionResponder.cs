// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Net;

namespace SimpleIAM.OpenIdAuthority.Orchestrators
{
    public abstract class ActionResponder
    {
        protected ActionResponse Ok(string message = null)
        {
            return new ActionResponse(message, HttpStatusCode.OK);
        }
        protected ActionResponse Redirect(string url)
        {
            return new ActionResponse(HttpStatusCode.Redirect) { RedirectUrl = url };
        }
        protected ActionResponse BadRequest(string message = null)
        {
            return new ActionResponse(message, HttpStatusCode.BadRequest);
        }
        protected ActionResponse Unauthenticated(string message = null)
        {
            return new ActionResponse(message, HttpStatusCode.Unauthorized);
        }
        protected ActionResponse PermissionDenied(string message = null)
        {
            return new ActionResponse(message, HttpStatusCode.Forbidden);
        }
        protected ActionResponse NotFound(string message = null)
        {
            return new ActionResponse(message, HttpStatusCode.NotFound);
        }
        protected ActionResponse Conflict(string message = null)
        {
            return new ActionResponse(message, HttpStatusCode.Conflict);
        }
        protected ActionResponse ServerError(string message = null)
        {
            return new ActionResponse(message, HttpStatusCode.InternalServerError);
        }
    }
}
