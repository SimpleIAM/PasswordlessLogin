using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleIAM.OpenIdAuthority.API
{
    public class AuthenticateApiResponse : ApiResponse
    {
        public string NextUrl { get; set; }
    }
}
