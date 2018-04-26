// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.IdAuthority.Configuration;
using SimpleIAM.IdAuthority.Services;
using SimpleIAM.IdAuthority.Services.Email;
using SimpleIAM.IdAuthority.Stores;

namespace SimpleIAM.IdAuthority.UI.Account
{
    [Route("")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IOneTimePasswordService _oneTimePasswordService;
        private readonly ISubjectStore _subjectStore;
        private readonly IdProviderConfig _config;
        private readonly IClientStore _clientStore;


        public AccountController(
            IIdentityServerInteractionService interaction,
            IEventService events,
            IEmailTemplateService emailTemplateService,
            IOneTimePasswordService oneTimePasswordService,
            ISubjectStore subjectStore,
            IdProviderConfig config,
            IClientStore clientStore)
        {
            _interaction = interaction;
            _events = events;
            _emailTemplateService = emailTemplateService;
            _oneTimePasswordService = oneTimePasswordService;
            _subjectStore = subjectStore;
            _config = config;
            _clientStore = clientStore;
        }

        [HttpGet("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(string returnUrl)
        {
            var viewModel = await GetSignInViewModel(returnUrl);
            return View(viewModel);
        }

        [HttpGet("signin")]
        [AllowAnonymous]
        public async Task<ActionResult> SignIn(string returnUrl)
        {
            var viewModel = await GetSignInViewModel(returnUrl);
            return View(viewModel);
        }

        [HttpPost("signin")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignIn(string returnUrl, SignInInputModel model)
        {
            if (ModelState.IsValid)
            {
                var oneTimePassword = await _oneTimePasswordService.CreateOneTimePasswordAsync(model.Email, TimeSpan.FromMinutes(5), returnUrl);
                var link = Url.Action("SignInLink", "Account", new { linkCode = oneTimePassword.LinkCode }, Request.Scheme);
                var fields = new Dictionary<string, string>()
                {
                    { "link", link },
                    { "one_time_password", oneTimePassword.OTP }
                };
                await _emailTemplateService.SendEmailAsync("SignInWithEmail", model.Email, fields);

                SaveUsernameHint(model.Email);

                return RedirectToAction("SignInLinkSent"); 
            }
            var viewModel = await GetSignInViewModel(returnUrl, model);
            return View(viewModel);
        }

        [HttpGet("signinlinksent")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInLinkSent(string returnUrl)
        {
            return View();
        }

        [HttpGet("signin/{linkCode}")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInLink(string linkCode)
        {
            if(linkCode != null && linkCode.Length < 36)
            {
                var oneTimePassword = await _oneTimePasswordService.UseOneTimeLinkAsync(linkCode);
                if (oneTimePassword == null)
                {
                    AddPostRedirectMessage("The sign in link is invalid.");
                    return RedirectToAction("SignIn");
                }
                if (oneTimePassword.ExpiresUTC < DateTime.UtcNow)
                {
                    AddPostRedirectMessage("The sign in link expired.");
                    //todo: consider if the redirect url should be kept...will it cause a correlation error?
                    return RedirectToAction("SignIn", new { returnUrl = oneTimePassword.RedirectUrl });
                }

                var subject = await _subjectStore.GetSubjectByEmailAsync(oneTimePassword.Email, true);
                await _events.RaiseAsync(new UserLoginSuccessEvent(subject.Email, subject.SubjectId, subject.Email));

                await HttpContext.SignInAsync(subject.SubjectId, subject.Email);

                if (_interaction.IsValidReturnUrl(oneTimePassword.RedirectUrl))
                {
                    return Redirect(oneTimePassword.RedirectUrl);
                }

                return Redirect("~/");
            }

            return NotFound();
        }

        [HttpGet("signinpass")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInPass(string returnUrl)
        {
            var viewModel = await GetSignInPassViewModel(returnUrl);
            return View(viewModel);
        }

        [HttpPost("signinpass")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignInPass(string returnUrl, SignInPassInputModel model)
        {
            if (ModelState.IsValid)
            {
                var oneTimePassword = await _oneTimePasswordService.UseOneTimePasswordAsync(model.Email);

                if (oneTimePassword == null || oneTimePassword.OTP != model.Password)
                {
                    ModelState.AddModelError("Password", "Invalid email or password");
                }
                else
                { 
                    if (oneTimePassword.ExpiresUTC < DateTime.UtcNow)
                    {
                        ModelState.AddModelError("Password", "The one time password has expired. Please request a new one.");
                    }
                    else
                    {
                        var subject = await _subjectStore.GetSubjectByEmailAsync(oneTimePassword.Email, true);
                        await _events.RaiseAsync(new UserLoginSuccessEvent(subject.Email, subject.SubjectId, subject.Email));

                        // handle custom session length
                        var authProps = (AuthenticationProperties)null;
                        var sessionLengthMinutes = model.SessionLengthMinutes ?? 0;
                        if (sessionLengthMinutes > 0 && sessionLengthMinutes < _config.MaxSessionLengthMinutes)
                        {
                            authProps = new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(sessionLengthMinutes))
                            };
                        };

                        await HttpContext.SignInAsync(subject.SubjectId, subject.Email, authProps);

                        SaveUsernameHint(subject.Email);

                        // use the redirect url saved with the one time password unless a different one was provided
                        returnUrl = returnUrl ?? oneTimePassword.RedirectUrl; 
                        if (_interaction.IsValidReturnUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }

                        return Redirect("~/");
                    }
                }
            }
            var viewModel = await GetSignInPassViewModel(returnUrl, model);
            return View(viewModel);
        }

        [HttpGet("AccountSettings")]
        public async Task<IActionResult> AccountSettings()
        {
            // account details screen
            // require a recent authentication in order to edit info
            var viewModel = new AccountSettingsViewModel()
            {
                Email = User.GetDisplayName()
            };
            return View(viewModel);
        }

        [HttpGet("signout")]
        [AllowAnonymous]
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

            return View(viewModel);
        }


        private async Task<SignInViewModel> GetSignInViewModel(string returnUrl, SignInInputModel model = null)
        {
            var viewModel = new SignInViewModel()
            {
                Email = model?.Email ?? GetUsernameHint(),
                LeaveBlank = model?.LeaveBlank,
                ReturnUrl = returnUrl,
            };

            return viewModel;
        }

        private async Task<SignInPassViewModel> GetSignInPassViewModel(string returnUrl, SignInPassInputModel model = null)
        {
            
            var viewModel = new SignInPassViewModel()
            {
                Email = model?.Email ?? GetUsernameHint(),
                LeaveBlank = model?.LeaveBlank,
                SessionLengthMinutes = model?.SessionLengthMinutes ?? _config.DefaultSessionLengthMinutes,
                ReturnUrl = returnUrl,
            };

            return viewModel;
        }

        private void AddPostRedirectMessage(string message)
        {
            var messages = TempData["PostRedirectMessages"];
            if(messages == null)
            {
                TempData["PostRedirectMessages"] = message;
            }
            else
            {
                TempData["PostRedirectMessages"] = $"{messages}|{message}";
            }
        }

        private void SaveUsernameHint(string email)
        {
            if(_config.RememberUsernames)
            {
                var options = new CookieOptions
                {
                    Expires = DateTime.Now.AddYears(1),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = true
                };
                Response.Cookies.Append("UsernameHint", email, options);
            }
        }

        private string GetUsernameHint()
        {
            if (_config.RememberUsernames)
            {
                var usernameHint = Request.Cookies["UsernameHint"];
                return usernameHint;
            }
            return null;
        }
    }
}