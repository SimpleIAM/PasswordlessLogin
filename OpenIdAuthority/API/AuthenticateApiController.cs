// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Orchestrators;
using SimpleIAM.OpenIdAuthority.Services;

namespace SimpleIAM.OpenIdAuthority.API
{
    [Route("api/v1")]
    [EnableCors("CorsPolicy")]
    public class AuthenticateApiController : Controller
    {
        private readonly AuthenticateOrchestrator _authenticateOrchestrator;
        private readonly IAuthorizedDeviceService _authorizedDeviceService;

        public AuthenticateApiController(
            AuthenticateOrchestrator authenticateOrchestrator,
            IAuthorizedDeviceService authorizedDeviceService
            )
        {
            _authenticateOrchestrator = authenticateOrchestrator;
            _authorizedDeviceService = authorizedDeviceService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]RegisterInputModel model)
        {
            if (ModelState.IsValid)
            {
                return SetNonceAndReturn(await _authenticateOrchestrator.RegisterAsync(model));
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        [HttpPost("send-one-time-code")]
        public async Task<IActionResult> SendOneTimeCode([FromBody] SendCodeInputModel model)
        {
            if (ModelState.IsValid)
            {
                return SetNonceAndReturn(await _authenticateOrchestrator.SendOneTimeCodeAsync(model));
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateInputModel model)
        {
            if (ModelState.IsValid)
            {
                var response = await _authenticateOrchestrator.AuthenticateCodeAsync(model, Request.GetDeviceId(), Request.GetClientNonce());
                if(response.StatusCode == 301)
                {
                    if (response.Content is SetDeviceIdCommand && model.StaySignedIn)
                    {
                        var deviceId = await _authorizedDeviceService.AuthorizeDevice(model.Username, Request.GetDeviceId(), Request.Headers["User-Agent"]);
                        if (deviceId != null)
                        {
                            Response.SetDeviceId(deviceId);
                        }
                    }
                    return await SignInAndReturnAsync(model.Username, model.StaySignedIn, response.RedirectUrl);
                }
                return response.ToJsonResult();
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        [DisableCors]
        [HttpPost("authenticate-password")]
        public async Task<IActionResult> AuthenticatePassword([FromBody] AuthenticatePasswordInputModel model)
        {
            if (ModelState.IsValid)
            {
                var response = await _authenticateOrchestrator.AuthenticatePasswordAsync(model, Request.GetDeviceId());
                if (response.StatusCode == 301)
                {
                    return await SignInAndReturnAsync(model.Username, model.StaySignedIn, response.RedirectUrl);
                }
                return response.ToJsonResult();
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        [HttpPost("send-password-reset-message")]
        public async Task<IActionResult> SendPasswordResetMessage([FromBody]SendPasswordResetMessageInputModel model)
        {
            if (ModelState.IsValid)
            {
                return SetNonceAndReturn(await _authenticateOrchestrator.SendPasswordResetMessageAsync(model));
            }
            return new ActionResponse(ModelState).ToJsonResult();
        }

        private IActionResult SetNonceAndReturn(ActionResponse result)
        {
            if (result.Content is SetClientNonceCommand)
            {
                Response.SetClientNonce((result.Content as SetClientNonceCommand).ClientNonce);
                result.Content = null;
            }
            return result.ToJsonResult();
        }

        private async Task<IActionResult> SignInAndReturnAsync(string username, bool staySignedIn, string nextUrl)
        {
            await _authenticateOrchestrator.SignInUserAsync(HttpContext, username, staySignedIn);
            return new JsonResult(new
            {
                Message = (string)null,
                NextUrl = nextUrl
            });
        }
    }
}