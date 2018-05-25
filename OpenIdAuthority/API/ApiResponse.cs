using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleIAM.OpenIdAuthority.API
{
    public class ApiResponse
    {
        public ApiResponse(string message = null)
        {
            Message = message;
        }

        public ApiResponse(ModelStateDictionary modelState, string message = null)
        {
            Message = message;
            // Extract error messages from model state. For form-level errors, use the key "_" instead of ""
            Errors = modelState
                .ToDictionary(x => x.Key, x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                .Where(em => em.Value.Count() > 0)
                .ToDictionary(x => x.Key == "" ? "_" : x.Key, x => x.Value);
            if(message == null) {
                var kvp = Errors.FirstOrDefault();
                Message = kvp.Value.FirstOrDefault();
                if(Message != null && kvp.Key != null && kvp.Key != "_") {
                    Message = $"{kvp.Key}: {Message}";
                }
            }
            else {
                Message = message;
            }
        }

        public string Message { get; set; }

        public Dictionary<string, string[]> Errors { get; set; }
    }
}
