// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.Stores;

namespace SimpleIAM.OpenIdAuthority.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly IAppStore _appStore;

        public HomeController(IAppStore appStore)
        {
            _appStore = appStore;
        }

        [HttpGet("")]
        public IActionResult Index()
        {            
            return View();
        }

        [HttpGet("apps")]
        [Authorize]
        public IActionResult Apps()
        {
            return View(_appStore.GetApps());
        }

        [HttpGet("error")]
        [HttpPost("error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}