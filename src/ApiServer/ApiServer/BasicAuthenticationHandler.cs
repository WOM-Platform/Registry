using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace WomPlatform.Web.Api {

    public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions> {

        protected DatabaseOperator Database { get; private set; }

        public BasicAuthenticationHandler(DatabaseOperator database, IOptionsMonitor<BasicAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) {

            Database = database;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
            if(!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader)) {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header not set or not readable"));
            }

            Logger.LogDebug("Authorization header: {0}", authorizationHeader);

            var authContent = Convert.FromBase64String(authorizationHeader[0].Substring(6));
            var authFields = System.Text.Encoding.ASCII.GetString(authContent).Split(':', StringSplitOptions.None);
            if(authFields.Length != 2) {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header invalid"));
            }

            var username = authFields[0];
            var password = authFields[1];
            var userProfile = Database.GetUserByLogin(username, password);
            if(userProfile == null) {
                return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
            }

            //var claim = new ClaimsPrincipal(new WomUserIdentity(userProfile));
            //new ClaimsIdentity()
            var t = new AuthenticationTicket(new ClaimsPrincipal(new WomUserIdentity(userProfile)), BasicAuthenticationSchemeOptions.DefaultScheme);
            return Task.FromResult(AuthenticateResult.Success(t));
        }

    }

}
