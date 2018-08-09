// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Configuration;

namespace SimpleIAM.PasswordlessLogin.UI.Shared
{
    public class BaseController : Controller
    {
        public BaseController()
        {            
        }

        protected void AddPostRedirectMessage(string message)
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

        protected void AddPostRedirectValue(string key, string value)
        {
            TempData[$"PostRedirect.{key}"] = value;
        }

        protected string GetPostRedirectValue(string key)
        {
            return (string)TempData[$"PostRedirect.{key}"];
        }
    }
}