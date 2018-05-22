// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc;

namespace SimpleIAM.OpenIdAuthority.API
{
    [Route("api/v1")]
    public class BaseApiController : Controller
    {
        protected IActionResult AuthenticationFailed(object obj)
        {
            return CustomResponse(obj, 401);
        }

        protected IActionResult Conflict(object obj)
        {
            return CustomResponse(obj, 409);
        }

        protected IActionResult ServerError(object obj)
        {
            return CustomResponse(obj, 500);
        }

        protected IActionResult CustomResponse(object obj, int statusCode)
        {
            return new JsonResult(obj) { StatusCode = statusCode };
        }        
    }
}