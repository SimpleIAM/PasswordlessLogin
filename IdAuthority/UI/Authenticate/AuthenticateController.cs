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
using SimpleIAM.IdAuthority.Entities;
using SimpleIAM.IdAuthority.Services.Email;
using SimpleIAM.IdAuthority.Services.OTC;
using SimpleIAM.IdAuthority.Services.Password;
using SimpleIAM.IdAuthority.Stores;

namespace SimpleIAM.IdAuthority.UI.Authenticate
{
    [Route("")]
    [Authorize]
    public class AuthenticateController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly ISubjectStore _subjectStore;
        private readonly IdProviderConfig _config;
        private readonly IClientStore _clientStore;
        private readonly IPasswordService _passwordService;


        public AuthenticateController(
            IIdentityServerInteractionService interaction,
            IEventService events,
            IEmailTemplateService emailTemplateService,
            IOneTimeCodeService oneTimeCodeService,
            ISubjectStore subjectStore,
            IdProviderConfig config,
            IClientStore clientStore,
            IPasswordService passwordService)
        {
            _interaction = interaction;
            _events = events;
            _emailTemplateService = emailTemplateService;
            _oneTimeCodeService = oneTimeCodeService;
            _subjectStore = subjectStore;
            _config = config;
            _clientStore = clientStore;
            _passwordService = passwordService;
        }

        [HttpGet("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(string returnUrl)
        {
            var viewModel = GetSignInViewModel(returnUrl);
            viewModel.Email = null;
            return View(viewModel);
        }

        [HttpGet("signin")]
        [AllowAnonymous]
        public async Task<ActionResult> SignIn(string returnUrl)
        {
            var viewModel = GetSignInViewModel(returnUrl);
            return View(viewModel);
        }

        [HttpPost("signin")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignIn(string returnUrl, SignInInputModel model)
        {
            if (ModelState.IsValid)
            {
                var oneTimeCode = await _oneTimeCodeService.CreateOneTimeCodeAsync(model.Email, TimeSpan.FromMinutes(5), returnUrl);
                var link = Url.Action("SignInLink", "Authenticate", new { linkCode = oneTimeCode.LinkCode }, Request.Scheme);
                var fields = new Dictionary<string, string>()
                {
                    { "link", link },
                    { "one_time_code", oneTimeCode.OTC }
                };
                await _emailTemplateService.SendEmailAsync("SignInWithEmail", model.Email, fields);

                SaveUsernameHint(model.Email);
                AddPostRedirectValue("Email", model.Email);

                return RedirectToAction("SignInCode"); 
            }
            var viewModel = GetSignInViewModel(returnUrl, model);
            return View(viewModel);
        }

        [HttpGet("signin/{linkCode}")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInLink(string linkCode)
        {
            if(linkCode != null && linkCode.Length < 36)
            {
                var oneTimeCode = await _oneTimeCodeService.UseOneTimeLinkAsync(linkCode);
                if (oneTimeCode == null)
                {
                    AddPostRedirectMessage("The sign in link is invalid.");
                    return RedirectToAction("SignIn");
                }
                if (oneTimeCode.ExpiresUTC < DateTime.UtcNow)
                {
                    AddPostRedirectMessage("The sign in link expired.");
                    //todo: consider if the redirect url should be kept...will it cause a correlation error?
                    return RedirectToAction("SignIn", new { returnUrl = oneTimeCode.RedirectUrl });
                }
                var subject = await _subjectStore.GetSubjectByEmailAsync(oneTimeCode.Email, true);
                return await FinishSignIn(subject, null, oneTimeCode.RedirectUrl);
            }

            return NotFound();
        }

        [HttpGet("signincode")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInCode()
        {
            var viewModel = GetSignInCodeViewModel();
            viewModel.Email = GetPostRedirectValue("Email");
            return View(viewModel);
        }

        [HttpPost("signincode")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignInCode(SignInCodeInputModel model)
        {
            if (ModelState.IsValid)
            {
                model.OneTimeCode = model.OneTimeCode.Trim();
                var oneTimeCode = await _oneTimeCodeService.UseOneTimeCodeAsync(model.Email);

                if (oneTimeCode == null || oneTimeCode.OTC != model.OneTimeCode)
                {
                    ModelState.AddModelError("OneTimeCode", "Invalid one time code");
                }
                else
                {
                    if (oneTimeCode.ExpiresUTC < DateTime.UtcNow)
                    {
                        ModelState.AddModelError("OneTimeCode", "The one time code has expired. Please request a new one.");
                        AddPostRedirectMessage("The one time code already expired. Please request a new one.");
                        AddPostRedirectValue("Email", model.Email);
                        return RedirectToAction("SignIn");
                    }
                    else
                    {
                        var subject = await _subjectStore.GetSubjectByEmailAsync(oneTimeCode.Email, true);
                        return await FinishSignIn(subject, model.SessionLengthMinutes, oneTimeCode.RedirectUrl);
                    }
                }
            }
            var viewModel = GetSignInCodeViewModel(model);
            return View(viewModel);
        }

        [HttpGet("signinpass")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInPass(string returnUrl)
        {
            var viewModel = GetSignInPassViewModel(returnUrl);
            return View(viewModel);
        }

        [HttpPost("signinpass")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SignInPass(string returnUrl, SignInPassInputModel model)
        {
            if (ModelState.IsValid)
            {
                var subject = await _subjectStore.GetSubjectByEmailAsync(model.Email, false);
                if (subject == null)
                {
                    ModelState.AddModelError("Password", "The email address or password wasn't right");
                }
                else
                {
                    var checkPasswordResult = await _passwordService.CheckPasswordAsync(subject.SubjectId, model.Password);
                    switch (checkPasswordResult)
                    {
                        case CheckPasswordResult.NotFound:
                        case CheckPasswordResult.PasswordIncorrect:
                            ModelState.AddModelError("Password", "The email address or password wasn't right");
                            break;
                        case CheckPasswordResult.TemporarilyLocked:
                            ModelState.AddModelError("Password", "Your password is temporarily locked. Try again later or sign in with email.");
                            break;
                        case CheckPasswordResult.ServiceFailure:
                            ModelState.AddModelError("Password", "Hmm. Something went wrong. Please try again.");
                            break;
                        case CheckPasswordResult.Success:
                            return await FinishSignIn(subject, model.SessionLengthMinutes, returnUrl);
                    }
                }
            }
            var viewModel = GetSignInPassViewModel(returnUrl, model);
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

        private async Task<ActionResult> FinishSignIn(Subject subject, int? sessionLengthMinutes, string returnUrl)
        {
            await _events.RaiseAsync(new UserLoginSuccessEvent(subject.Email, subject.SubjectId, subject.Email));

            // handle custom session length
            var authProps = (AuthenticationProperties)null;
            var sessionLengthMinutesInt = sessionLengthMinutes ?? 0;
            if (sessionLengthMinutes > 0 && sessionLengthMinutes < _config.MaxSessionLengthMinutes)
            {
                authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(sessionLengthMinutesInt))
                };
            };

            await HttpContext.SignInAsync(subject.SubjectId, subject.Email, authProps);

            SaveUsernameHint(subject.Email);

            if (_interaction.IsValidReturnUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Apps", "Home");
        }

        private SignInViewModel GetSignInViewModel(string returnUrl, SignInInputModel model = null)
        {
            var viewModel = new SignInViewModel()
            {
                Email = model?.Email ?? GetUsernameHint(),
                LeaveBlank = model?.LeaveBlank,
                ReturnUrl = returnUrl,
            };

            return viewModel;
        }

        private SignInCodeViewModel GetSignInCodeViewModel(SignInCodeInputModel model = null)
        {

            var viewModel = new SignInCodeViewModel()
            {
                Email = model?.Email ?? GetUsernameHint(),
                OneTimeCode = model?.OneTimeCode,
                LeaveBlank = model?.LeaveBlank,
                SessionLengthMinutes = model?.SessionLengthMinutes ?? _config.DefaultSessionLengthMinutes
            };

            return viewModel;
        }

        private SignInPassViewModel GetSignInPassViewModel(string returnUrl = null, SignInPassInputModel model = null)
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
            var messages = GetPostRedirectValue("Messages");
            if(messages == null)
            {
                AddPostRedirectValue("Messages", message);
            }
            else
            {
                AddPostRedirectValue("Messages", $"{messages}|{message}");
            }
        }

        private void AddPostRedirectValue(string key, string value)
        {
            TempData[$"PostRedirect.{key}"] = value;
        }

        private string GetPostRedirectValue(string key)
        {
            return (string)TempData[$"PostRedirect.{key}"];
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
                    //Secure = true //todo: consider pros and cons of enabling this
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