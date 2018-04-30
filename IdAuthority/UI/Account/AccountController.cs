// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.IdAuthority.Services.Email;
using SimpleIAM.IdAuthority.Services.Password;
using SimpleIAM.IdAuthority.Stores;

namespace SimpleIAM.IdAuthority.UI.Authenticate
{
    [Route("")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ISubjectStore _subjectStore;
        private readonly IPasswordService _passwordService;


        public AccountController(
            IEmailTemplateService emailTemplateService,
            ISubjectStore subjectStore,
            IPasswordService passwordService)
        {
            _emailTemplateService = emailTemplateService;
            _subjectStore = subjectStore;
            _passwordService = passwordService;
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

    }
}