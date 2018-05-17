// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Services.Email;
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
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ISubjectStore _subjectStore;
        private readonly IPasswordService _passwordService;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IdProviderConfig _config;

        public AccountController(            
            IEmailTemplateService emailTemplateService,
            ISubjectStore subjectStore,
            IPasswordService passwordService,
            IOneTimeCodeService oneTimeCodeService,
            IdProviderConfig config)
        {
            _emailTemplateService = emailTemplateService;
            _subjectStore = subjectStore;
            _passwordService = passwordService;
            _oneTimeCodeService = oneTimeCodeService;
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
        public IActionResult SetPassword()
        {
            var viewModel = GetSetPasswordViewModel();
            return View(viewModel);
        }

        [HttpPost("setpassword")]
        public async Task<IActionResult> SetPassword(SetPasswordModel model)
        {
            var sub = User.GetSubjectId();

            var result = await _passwordService.SetPasswordAsync(sub, model.NewPassword);
            switch (result)
            {
                case SetPasswordResult.Success:
                    AddPostRedirectMessage("Password successfully set");
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
                var checkOtcResponse = await _oneTimeCodeService.CheckOneTimeCodeAsync(email, model.OneTimeCode);
                if (checkOtcResponse.Result != CheckOneTimeCodeResult.Verified)
                {
                    ModelState.AddModelError("OneTimeCode", "Code is incorrect or expired");
                }
                else
                {
                    var sub = User.GetSubjectId();
                    var result = await _passwordService.RemovePasswordAsync(sub);
                    if (result == RemovePasswordResult.Success)
                    {
                        AddPostRedirectMessage("Password successfully removed");
                        return RedirectToAction("MyAccount");
                    }
                    AddPostRedirectMessage("Something was wrong");
                    return RedirectToAction("RemovePassword");
                }
            }
            return View(model);
        }

        private async Task<bool> SendOneTimeCodeBeforeRedirect()
        {
            var email = User.GetDisplayName();

            var response = await _oneTimeCodeService.SendOneTimeCodeAsync(email, TimeSpan.FromMinutes(5));
            switch (response.Result)
            {
                case SendOneTimeCodeResult.Sent:
                    AddPostRedirectMessage("We sent a one time code to your email address");
                    return true;
                case SendOneTimeCodeResult.TooManyRequests:
                    AddPostRedirectMessage("We recently sent a one time code to your email address. If you didn't get it, please go back and request a new code in a few minutes.");
                    return true;
                default:
                case SendOneTimeCodeResult.InvalidRequest:
                case SendOneTimeCodeResult.ServiceFailure:
                    var endUserErrorMessage = response.MessageForEndUser ?? "Something went wrong and we were unable to send you a one time code";
                    ModelState.AddModelError("GetOneTimeCode", endUserErrorMessage);
                    return false;
            }
        }

        private SetPasswordModel GetSetPasswordViewModel(SetPasswordModel inputModel = null)
        {
            var viewModel = new SetPasswordModel() {
                MinimumPasswordStrengthInBits = _config.MinimumPasswordStrengthInBits,
                OneTimeCode = inputModel?.OneTimeCode
            };
            return viewModel;
        }
    }
}