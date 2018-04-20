// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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


        public AccountController(
            IIdentityServerInteractionService interaction,
            IEventService events,
            IEmailTemplateService emailTemplateService,
            IOneTimePasswordService oneTimePasswordService,
            ISubjectStore subjectStore)
        {
            _interaction = interaction;
            _events = events;
            _emailTemplateService = emailTemplateService;
            _oneTimePasswordService = oneTimePasswordService;
            _subjectStore = subjectStore;
        }

        [HttpGet("account")]
        public async Task<IActionResult> Index()
        {
            // account details screen
            // require a recent authentication in order to edit info
            return View();
        }

        [HttpGet("signin")]
        [AllowAnonymous]
        public async Task<ActionResult> SignIn(string returnUrl)
        {
            var viewModel = await GetSignInViewModel(returnUrl);
            return View(viewModel);
        }

        [HttpGet("signinlink/{linkCode}")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInLink(string linkCode)
        {
            if(linkCode != null && linkCode.Length < 36)
            {
                var oneTimePassword = await _oneTimePasswordService.UseOneTimeLinkAsync(linkCode);
                if (oneTimePassword == null)
                {
                    ModelState.AddModelError("", "The sign in link is invalid. Please request a new one.");
                    return View();
                }
                if (oneTimePassword.ExpiresUTC < DateTime.UtcNow)
                {
                    ModelState.AddModelError("", "The sign in link has expired. Please request a new one.");
                    return View();
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

        [HttpPost("signin")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignIn(string returnUrl, SignInInputModel model)
        {
            var signInEmailSent = false;
            if (ModelState.IsValid)
            {
                if(model.Password == null)
                {
                    var oneTimePassword = await _oneTimePasswordService.CreateOneTimePasswordAsync(model.Email, TimeSpan.FromMinutes(5), returnUrl);
                    var link = Url.Action("SignInLink", "Account", new { linkCode = oneTimePassword.LinkCode }, Request.Scheme);
                    var fields = new Dictionary<string, string>()
                    {
                        { "link", link },
                        { "one_time_password", oneTimePassword.OTP }
                    };
                    await _emailTemplateService.SendEmailAsync("SignInWithEmail", model.Email, fields);
                    signInEmailSent = true;
                }
                else
                {
                    var oneTimePassword = await _oneTimePasswordService.UseOneTimePasswordAsync(model.Email);

                    if (oneTimePassword != null && oneTimePassword.OTP == model.Password)
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
                            var maxSessionLengthMinutes = 1440; //todo: move to a config setting
                            var sessionLengthMinutes = model.SessionLengthMinutes ?? 0;
                            if (sessionLengthMinutes > 0 && sessionLengthMinutes < maxSessionLengthMinutes)
                            {
                                authProps = new AuthenticationProperties
                                {
                                    IsPersistent = true,
                                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(sessionLengthMinutes))
                                };
                            };

                            await HttpContext.SignInAsync(subject.SubjectId, subject.Email, authProps);

                            if (_interaction.IsValidReturnUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }

                            return Redirect("~/");
                        }
                    }
                }
            }
            var viewModel = await GetSignInViewModel(returnUrl, model, signInEmailSent);
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
            }

            var viewModel = new SignedOutViewModel()
            {
                AppName = context?.ClientName ?? "the website", //todo: note that this can be blank, so need to look up client name
                PostLogoutRedirectUri = context?.PostLogoutRedirectUri,
                SignOutIFrameUrl = context?.SignOutIFrameUrl
            };

            return View(viewModel);
        }


        private async Task<SignInViewModel> GetSignInViewModel(string returnUrl, SignInInputModel model = null, bool signInEmailSent = false)
        {
            var viewModel = new SignInViewModel()
            {
                Email = model?.Email,
                LeaveBlank = model?.LeaveBlank,
                SessionLengthMinutes = model?.SessionLengthMinutes,
                SignInEmailSent = signInEmailSent,
                ClientName = "??", // todo: fill in the value
            };

            return viewModel;
        }
    }
}