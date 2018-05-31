// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority.Orchestrators
{
    public abstract class ActionResponder
    {
        protected ActionResponse Ok(string message = null)
        {
            return new ActionResponse(message, 200);
        }
        protected ActionResponse Redirect(string url)
        {
            return new ActionResponse(301) { RedirectUrl = url };
        }
        protected ActionResponse BadRequest(string message = null)
        {
            return new ActionResponse(message, 400);
        }
        protected ActionResponse Unauthenticated(string message = null)
        {
            return new ActionResponse(message, 401);
        }
        protected ActionResponse NotFound(string message = null)
        {
            return new ActionResponse(message, 404);
        }
        protected ActionResponse Conflict(string message = null)
        {
            return new ActionResponse(message, 409);
        }
        protected ActionResponse ServerError(string message = null)
        {
            return new ActionResponse(message, 500);
        }
    }
}
