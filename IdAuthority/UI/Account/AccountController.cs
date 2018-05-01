// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.IdAuthority.Services.Email;
using SimpleIAM.IdAuthority.Services.OTC;
using SimpleIAM.IdAuthority.Services.Password;
using SimpleIAM.IdAuthority.Stores;
using SimpleIAM.IdAuthority.UI.Account;
using SimpleIAM.IdAuthority.UI.Shared;

namespace SimpleIAM.IdAuthority.UI.Authenticate
{
    [Route("account")]
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ISubjectStore _subjectStore;
        private readonly IPasswordService _passwordService;
        private readonly IOneTimeCodeService _oneTimeCodeService;

        public AccountController(            
            IEmailTemplateService emailTemplateService,
            ISubjectStore subjectStore,
            IPasswordService passwordService,
            IOneTimeCodeService oneTimeCodeService)
        {
            _emailTemplateService = emailTemplateService;
            _subjectStore = subjectStore;
            _passwordService = passwordService;
            _oneTimeCodeService = oneTimeCodeService;
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
        public async Task<IActionResult> SetPassword()
        {
            var viewModel = new SetPasswordModel();
            return View(viewModel);
        }

        [HttpPost("setpassword")]
        public async Task<IActionResult> SetPassword(SetPasswordModel model)
        {
            var sub = User.GetSubjectId();

            var email = User.GetDisplayName();

            if(model.GetOneTimeCode)
            {
                ModelState.Clear();
                var result = await _oneTimeCodeService.SendOneTimeCodeAsync(email, TimeSpan.FromMinutes(5));
                switch(result)
                {
                    case SendOneTimeCodeResult.Sent:
                        ModelState.AddModelError("GetOneTimeCode", "We sent a code to your email address");
                        break;
                    case SendOneTimeCodeResult.InvalidRequest:
                    case SendOneTimeCodeResult.ServiceFailure:
                    case SendOneTimeCodeResult.TooManyRequests:
                        ModelState.AddModelError("GetOneTimeCode", "Fail"); //todo: refine
                        break;
                }
            }
            else if (ModelState.IsValid)
            {
                var checkOtcResponse = await _oneTimeCodeService.CheckOneTimeCodeAsync(email, model.OneTimeCode);
                if (checkOtcResponse.Result != CheckOneTimeCodeResult.Verified)
                {
                    ModelState.AddModelError("OneTimeCode", "Fail"); //todo: refine
                }
                else
                {
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
                }
            }

            var viewModel = new SetPasswordModel
            {
                OneTimeCode = model.OneTimeCode
            };
            return View(viewModel);
        }

        [HttpGet("removepassword")]
        public async Task<IActionResult> RemovePassword()
        {
            return View();
        }

        [HttpPost("removepassword")]
        public async Task<IActionResult> RemovePassword(RemovePasswordModel model)
        {
            var email = User.GetDisplayName();

            if (model.GetOneTimeCode)
            {
                ModelState.Clear();
                var result = await _oneTimeCodeService.SendOneTimeCodeAsync(email, TimeSpan.FromMinutes(5));
                switch (result)
                {
                    case SendOneTimeCodeResult.Sent:
                        ModelState.AddModelError("GetOneTimeCode", "We sent a code to your email address");
                        break;
                    case SendOneTimeCodeResult.InvalidRequest:
                    case SendOneTimeCodeResult.ServiceFailure:
                    case SendOneTimeCodeResult.TooManyRequests:
                        ModelState.AddModelError("GetOneTimeCode", "Fail"); //todo: refine
                        break;
                }
            }
            else if (ModelState.IsValid)
            {
                var checkOtcResponse = await _oneTimeCodeService.CheckOneTimeCodeAsync(email, model.OneTimeCode);
                if (checkOtcResponse.Result != CheckOneTimeCodeResult.Verified)
                {
                    ModelState.AddModelError("OneTimeCode", "Fail"); //todo: refine
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
    }
}