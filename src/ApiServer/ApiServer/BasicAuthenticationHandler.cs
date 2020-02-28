using System;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace WomPlatform.Web.Api {

    public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions> {

        private readonly MongoDatabase _mongo;

        public BasicAuthenticationHandler(
            MongoDatabase mongo,
            IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _mongo = mongo;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            if(!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader)) {
                return AuthenticateResult.Fail("Authorization header not set or not readable");
            }

            Logger.LogDebug("Authorization header: {0}", authorizationHeader);

            var authContent = Convert.FromBase64String(authorizationHeader[0].Substring(6));
            var authFields = System.Text.Encoding.ASCII.GetString(authContent).Split(':', StringSplitOptions.None);
            if(authFields.Length != 2) {
                return AuthenticateResult.Fail("Authorization header invalid");
            }

            var email = authFields[0];
            var password = authFields[1];

            var userProfile = await _mongo.GetUserByEmail(email);
            if(userProfile == null) {
                return AuthenticateResult.Fail("Invalid username or password");
            }
            if(!BCrypt.Net.BCrypt.Verify(password, userProfile.PasswordHash)) {
                return AuthenticateResult.Fail("Invalid username or password");
            }

            var identity = new WomUserIdentity(userProfile);

            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                BasicAuthenticationSchemeOptions.DefaultScheme
            );
            return AuthenticateResult.Success(ticket);
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties) {
            if(!Request.Headers.ContainsKey(HeaderNames.Authorization)) {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                Response.Headers.Add(HeaderNames.WWWAuthenticate,
                    new StringValues("Basic realm=\"WOM\"")
                );
            }
            else {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }

            return Task.CompletedTask;
        }

    }

}
