// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.IdAuthority.Stores;

namespace SimpleIAM.IdAuthority.Controllers
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
        [Authorize]
        public IActionResult Index()
        {            
            return View(_appStore.GetApps());
        }

        [Route("error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}