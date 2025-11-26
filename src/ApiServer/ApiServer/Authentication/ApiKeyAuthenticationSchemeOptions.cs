using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace WomPlatform.Web.Api.Authentication {
    public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions {
        public const string SchemeName = "WOM API key authentication";

        public string Scheme => SchemeName;

        public StringValues AuthKey { get; set; }
    }
}
