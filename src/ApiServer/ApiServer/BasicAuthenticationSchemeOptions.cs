using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace WomPlatform.Web.Api {

    public class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions {

        public const string DefaultScheme = "Custom WOM auth";

        public string Scheme => DefaultScheme;

        public StringValues AuthKey { get; set; }

    }

}
