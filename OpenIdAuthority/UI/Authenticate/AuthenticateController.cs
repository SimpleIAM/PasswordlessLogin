// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Orchestrators;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.UI.Shared;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    [Route("")]
    public class AuthenticateController : BaseController
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IClientStore _clientStore;
        private readonly AuthenticateOrchestrator _authenticateOrchestrator;

        public AuthenticateController(
            AuthenticateOrchestrator authenticateOrchestrator,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IOneTimeCodeService oneTimeCodeService,
            IClientStore clientStore)
        {
            _authenticateOrchestrator = authenticateOrchestrator;
            _interaction = interaction;
            _events = events;
            _oneTimeCodeService = oneTimeCodeService;
            _clientStore = clientStore;
        }

        [HttpGet("register")]
        public ActionResult Register(string returnUrl)
        {
            return View(new RegisterInputModel());
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterInputModel model, bool consent, string leaveBlank)
        {
            if(leaveBlank != null) 
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (ModelState.IsValid)
            {
                if(!consent) 
                {
                    ViewBag.Message = "Please acknowledge your consent";
                }
                else 
                {
                    var response = await _authenticateOrchestrator.RegisterAsync(model);
                    ViewBag.Message = response.Message;
                }
            }
            return View(model);
        }

        [HttpGet("forgotpassword")]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("forgotpassword")]
        public async Task<ActionResult> ForgotPassword(SendPasswordResetMessageInputModel model, string leaveBlank)
        {
            if(leaveBlank != null) 
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (ModelState.IsValid)
            {
                var response = await _authenticateOrchestrator.SendPasswordResetMessageAsync(model);
                ViewBag.Message = response.Message;
            }
            return View(model);
        }

        [HttpGet("signin")]
        public async Task<ActionResult> SignIn(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            var viewModel = new AuthenticatePasswordInputModel()
            {
                Username = context?.LoginHint,
                NextUrl = returnUrl,
            };

            return View(viewModel);
        }

        [HttpPost("signin")]
        public async Task<ActionResult> SignIn(AuthenticatePasswordInputModel model, string action, string leaveBlank)
        {            
            if (leaveBlank != null) 
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (model.Username != null && (action == "getcode" || (action != "signin" && model.Password == null)))
            {
                ModelState.ClearValidationState("Password");
                var context = await _interaction.GetAuthorizationContextAsync(model.NextUrl);
                var input = new SendCodeInputModel()
                {
                    ApplicationId = context?.ClientId,
                    Username = model.Username,
                    NextUrl = model.NextUrl
                };
                var response = await _authenticateOrchestrator.SendOneTimeCodeAsync(input);
                ViewBag.Message = response.Message;
            }
            else if (ModelState.IsValid)
            {
                var response = await _authenticateOrchestrator.AuthenticateAsync(model);
                if (response.StatusCode == 301)
                {
                    await _authenticateOrchestrator.SignInUserAsync(HttpContext, model.Username, model.StaySignedIn);
                    return Redirect(response.RedirectUrl);
                }
                ViewBag.Message = response.Message;
            }
            return View(model);
        }
        
        [HttpGet("signin/{longCode}")]
        public async Task<ActionResult> SignInLink(string longCode)
        {
            var response = await _authenticateOrchestrator.AuthenticateLongCodeAsync(longCode);
            switch(response.StatusCode)
            {
                case 301:
                    var username = response.Message;
                    await _authenticateOrchestrator.SignInUserAsync(HttpContext, username, false);
                    return Redirect(response.RedirectUrl);
                case 404:
                    return NotFound();
                default:
                    AddPostRedirectMessage(response.Message);
                    return RedirectToAction("SignIn");
            }
        }

        [HttpGet("signout")]
        public async Task<ActionResult> SignOut(string id)
        {
            var context = await _interaction.GetLogoutContextAsync(id);

            if (User?.Identity.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync();
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
                
                // We're signed out now, so the UI for this request should show an anonymous user
                HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }
            var viewModel = new SignedOutViewModel()
            {
                AppName = (await _clientStore.FindEnabledClientByIdAsync(context?.ClientId))?.ClientName ?? "the website",
                PostLogoutRedirectUri = context?.PostLogoutRedirectUri,
                SignOutIFrameUrl = context?.SignOutIFrameUrl
            };

            return View("SignedOut", viewModel);
        }
    }
}