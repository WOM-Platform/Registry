using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace WomPlatform.Web.Api.Authentication {

    public class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions {

        public const string SchemeName = "WOM basic authentication";

        public string Scheme => SchemeName;

        public StringValues AuthKey { get; set; }

    }

}
