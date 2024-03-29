﻿using System;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api {

    public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions> {

        private readonly UserService _userService;

        public BasicAuthenticationHandler(
            UserService userService,
            IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _userService = userService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            if(!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader)) {
                return AuthenticateResult.NoResult();
            }
            if(!authorizationHeader[0].StartsWith("Basic ")) {
                return AuthenticateResult.NoResult();
            }

            var authContent = Convert.FromBase64String(authorizationHeader[0][6..]);
            var authFields = System.Text.Encoding.ASCII.GetString(authContent).Split(':', StringSplitOptions.None);
            if(authFields.Length != 2) {
                return AuthenticateResult.Fail("Authorization header invalid");
            }

            var email = authFields[0];
            var password = authFields[1];

            var userProfile = await _userService.GetUserByEmail(email);
            if(userProfile == null) {
                return AuthenticateResult.Fail("Invalid username or password");
            }
            if(!BCrypt.Net.BCrypt.Verify(password, userProfile.PasswordHash)) {
                return AuthenticateResult.Fail("Invalid username or password");
            }

            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(new BasicIdentity(userProfile.Id, email)),
                BasicAuthenticationSchemeOptions.SchemeName
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
