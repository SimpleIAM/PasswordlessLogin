﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Services.Email;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;
using SimpleIAM.PasswordlessLogin.UI.Account;
using SimpleIAM.PasswordlessLogin.UI.Shared;

namespace SimpleIAM.PasswordlessLogin.UI.Authenticate
{
    [Route("account")]
    [Authorize]
    public class AccountController : PasswordlessBaseController
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly IdProviderConfig _config;

        public AccountController(
            IEmailTemplateService emailTemplateService,
            IUserStore userStore,
            IPasswordService passwordService,
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            IdProviderConfig config)
        {
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
                    return RedirectToAction(nameof(MyAccount));
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
                    return RedirectToAction(nameof(RemovePassword), new { step = 2 });
                }
                return RedirectToAction(nameof(RemovePassword));
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
                            return RedirectToAction(nameof(MyAccount));
                        }
                        AddPostRedirectMessage("Something was wrong");
                        return RedirectToAction(nameof(RemovePassword));
                    case CheckOneTimeCodeResult.VerifiedWithoutNonce:
                    default:
                        ModelState.AddModelError(nameof(model.OneTimeCode), "Code is incorrect or expired");
                        break;
                }
            }
            return View(model);
        }

        private async Task<bool> SendOneTimeCodeBeforeRedirect()
        {
            var email = User.GetDisplayName();

            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(email, 
                TimeSpan.FromMinutes(PasswordlessLoginConstants.OneTimeCode.DefaultValidityMinutes));
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