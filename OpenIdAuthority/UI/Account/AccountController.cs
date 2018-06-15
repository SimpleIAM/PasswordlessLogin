// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Services.Email;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;
using SimpleIAM.OpenIdAuthority.UI.Account;
using SimpleIAM.OpenIdAuthority.UI.Shared;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    [Route("account")]
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly IdProviderConfig _config;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IEmailTemplateService emailTemplateService,
            IUserStore userStore,
            IPasswordService passwordService,
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            IdProviderConfig config)
        {
            _interaction = interaction;
            _emailTemplateService = emailTemplateService;
            _userStore = userStore;
            _passwordService = passwordService;
            _oneTimeCodeService = oneTimeCodeService;
            _messageService = messageService;
            _config = config;
        }

        [HttpGet("")]
        public async Task<IActionResult> MyAccount()
        {
            // account details screen
            // require a recent authentication in order to edit info
            var email = User.GetDisplayName();
            var sub = User.GetSubjectId();

            var paswordLastChangedUTC = await _passwordService.PasswordLastChangedAsync(sub);
            var viewModel = new MyAccountViewModel()
            {
                Email = email,
                PaswordLastChangedUTC = paswordLastChangedUTC,
            };
            return View(viewModel);
        }

        [HttpGet("setpassword")]
        public IActionResult SetPassword(string nextUrl)
        {
            var viewModel = GetSetPasswordViewModel(null, nextUrl);
            return View(viewModel);
        }

        [HttpPost("setpassword")]
        public async Task<IActionResult> SetPassword(SetPasswordModel model, string skip)
        {
            if(skip != null && model.NextUrl != null && (Url.IsLocalUrl(model.NextUrl) || true /*todo: validate that is is a url for a registered client? maybe use IRedirectUriValidator.IsRedirectUriValidAsync*/))
            {
                return Redirect(model.NextUrl);
            }
            var sub = User.GetSubjectId();

            var result = await _passwordService.SetPasswordAsync(sub, model.NewPassword);
            switch (result)
            {
                case SetPasswordResult.Success:
                    AddPostRedirectMessage("Password successfully set");
                    
                    if(model.NextUrl != null && (Url.IsLocalUrl(model.NextUrl) || true /*todo: validate that is is a url for a registered client?*/))
                    {
                        return Redirect(model.NextUrl);
                    }
                    return RedirectToAction("MyAccount");
                case SetPasswordResult.PasswordDoesNotMeetStrengthRequirements:
                    ModelState.AddModelError("NewPassword", "Password does not meet minimum password strength requirements (try something longer).");
                    break;
                case SetPasswordResult.ServiceFailure:
                    ModelState.AddModelError("NewPassword", "Something went wrong.");
                    break;
            }

            var viewModel = GetSetPasswordViewModel(model);
            return View(viewModel);
        }

        [HttpGet("removepassword")]
        public IActionResult RemovePassword(int step = 1)
        {
            switch (step)
            {
                case 1:
                    return View("GetOneTimeCode");
                case 2:
                    return View();
                default:
                    return NotFound();
            }
        }

        [HttpPost("removepassword")]
        public async Task<IActionResult> RemovePassword(RemovePasswordModel model, bool getOneTimeCode = false)
        {
            var email = User.GetDisplayName();

            if (getOneTimeCode)
            {
                var success = await SendOneTimeCodeBeforeRedirect();
                if (success)
                {
                    return RedirectToAction("RemovePassword", new { step = 2 });
                }
                return RedirectToAction("RemovePassword");
            }
            else if (ModelState.IsValid)
            {
                var checkOtcResponse = await _oneTimeCodeService.CheckOneTimeCodeAsync(email, model.OneTimeCode, Request.GetClientNonce());
                switch(checkOtcResponse.Result) {
                    case CheckOneTimeCodeResult.VerifiedWithNonce:
                        var sub = User.GetSubjectId();
                        var result = await _passwordService.RemovePasswordAsync(sub);
                        if (result == RemovePasswordResult.Success)
                        {
                            AddPostRedirectMessage("Password successfully removed");
                            return RedirectToAction("MyAccount");
                        }
                        AddPostRedirectMessage("Something was wrong");
                        return RedirectToAction("RemovePassword");
                    case CheckOneTimeCodeResult.VerifiedWithoutNonce:
                    default:
                        ModelState.AddModelError("OneTimeCode", "Code is incorrect or expired");
                        break;
                }
            }
            return View(model);
        }

        private async Task<bool> SendOneTimeCodeBeforeRedirect()
        {
            var email = User.GetDisplayName();

            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(email, TimeSpan.FromMinutes(5));
            switch (oneTimeCodeResponse.Result)
            {
                case GetOneTimeCodeResult.Success:
                    if (oneTimeCodeResponse.ClientNonce != null)
                    {
                        Response.SetClientNonce(oneTimeCodeResponse.ClientNonce);
                    }
                    var response = await _messageService.SendOneTimeCodeMessageAsync(null, email, oneTimeCodeResponse.ShortCode);
                    if (!response.MessageSent)
                    {
                        var endUserErrorMessage = response.ErrorMessageForEndUser ?? "Something went wrong and we were unable to send you a one time code";
                        ModelState.AddModelError("GetOneTimeCode", endUserErrorMessage);
                        return false;
                    }
                    break;
                case GetOneTimeCodeResult.TooManyRequests:
                    AddPostRedirectMessage("We recently sent a one time code to your email address. If you didn't get it, please go back and request a new code in a few minutes.");
                    return true;
                case GetOneTimeCodeResult.ServiceFailure:
                default:
                    ModelState.AddModelError("GetOneTimeCode", "Something went wrong and we were unable to send you a one time code");
                    return false;
            }

            AddPostRedirectMessage("We sent a one time code to your email address");
            return true;
        }

        private SetPasswordModel GetSetPasswordViewModel(SetPasswordModel inputModel = null, string nextUrl = null)
        {
            var viewModel = new SetPasswordModel() {
                MinimumPasswordStrengthInBits = _config.MinimumPasswordStrengthInBits,
                OneTimeCode = inputModel?.OneTimeCode,
                NextUrl = inputModel?.NextUrl ?? nextUrl
            };
            return viewModel;
        }
    }
}