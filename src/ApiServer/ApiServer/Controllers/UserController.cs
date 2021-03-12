using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/user")]
    [RequireHttps]
    [OperationsTags("User and session management")]
    public class UserController : BaseRegistryController {

        private readonly MailComposer _composer;

        public UserController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<UserController> logger,
            MailComposer composer
        ) : base(configuration, crypto, keyManager, mongo, @operator, logger) {
            _composer = composer;
        }

        /// <summary>
        /// Check whether a user password is acceptable.
        /// </summary>
        private bool CheckPassword(string password) {
            var userSecuritySection = Configuration.GetSection("UserSecurity");
            var minLength = Convert.ToInt32(userSecuritySection["MinimumUserPasswordLength"]);

            if(string.IsNullOrWhiteSpace(password)) {
                return false;
            }

            if(password.Length < minLength) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// User registration payload.
        /// </summary>
        public record UserRegisterInput(
            string Email, string Password, string Name, string Surname
        );

        /// <summary>
        /// Register a new user to the service.
        /// </summary>
        /// <param name="input">User registration payload.</param>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(UserRegisterInput input) {
            var existingUser = await Mongo.GetUserByEmail(input.Email);
            if(existingUser != null) {
                return this.ProblemParameter("Supplied email address is already registered");
            }

            if(!CheckPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            var verificationToken = new Random().GenerateReadableCode(8);
            Logger.LogDebug("Registering new user for {0} with verification token {1}", input.Email, verificationToken);

            try {
                var user = new User {
                    Email = input.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                    Name = input.Name,
                    Surname = input.Surname,
                    VerificationToken = verificationToken,
                    RegisteredOn = DateTime.UtcNow
                };
                await Mongo.CreateUser(user);

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        id = user.Id.ToString()
                    },
                    new UserOutput {
                        Id = user.Id.ToString(),
                        Email = user.Email,
                        Name = user.Name,
                        Surname = user.Surname
                    }
                );
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to register new user with email {0}", input.Email);
                throw;
            }
        }

        /// <summary>
        /// Retrieves information about an existing user.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            var existingUser = await Mongo.GetUserById(id);
            if(existingUser == null) {
                return NotFound();
            }

            return Ok(new UserOutput {
                Id = existingUser.Id.ToString(),
                Email = existingUser.Email,
                Name = existingUser.Name,
                Surname = existingUser.Surname
            });
        }

        public record UserUpdateInformationInput(string Email, string Name, string Surname, string Password);

        /// <summary>
        /// Updates information about an existing user.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="input">User information payload.</param>
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateInformation(
            [FromRoute] ObjectId id,
            UserUpdateInformationInput input
        ) {
            if(!User.UserIdEquals(id)) {
                return Forbid();
            }

            var existingUser = await Mongo.GetUserById(id);
            if(existingUser == null) {
                return NotFound();
            }

            if(input.Password != null && !CheckPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            try {
                if(input.Name != null) {
                    existingUser.Name = input.Name;
                }
                if(input.Surname != null) {
                    existingUser.Surname = input.Surname;
                }
                if(input.Password != null) {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password);
                }
                existingUser.LastUpdate = DateTime.UtcNow;

                await Mongo.ReplaceUser(existingUser);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update user {0}", id);
                throw;
            }

            return Ok();
        }

        public record UserVerifyInput(string Token);

        /// <summary>
        /// Verifies a user account.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="input">User verification payload.</param>
        [HttpPost("{id}/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Verify(
            [FromRoute] ObjectId id,
            UserVerifyInput input
        ) {
            var user = await Mongo.GetUserById(id);
            if(user == null) {
                return NotFound();
            }

            if(user.VerificationToken != input.Token) {
                return NotFound();
            }

            user.VerificationToken = null;
            await Mongo.ReplaceUser(user);

            return Ok();
        }

        public record UserRequestPasswordResetInput(string Email);

        /// <summary>
        /// Requests a password reset for an existing user.
        /// </summary>
        /// <param name="input">Password request payload.</param>
        [HttpPost("password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestPasswordReset(
            UserRequestPasswordResetInput input
        ) {
            var user = await Mongo.GetUserByEmail(input.Email);
            if(user != null) {
                if(user.PasswordResetToken == null) {
                    user.PasswordResetToken = new Random().GenerateReadableCode(8);
                    await Mongo.ReplaceUser(user);
                }

                _composer.SendPasswordResetMail(user);
            }

            return Ok();
        }

        public record UserExecutePasswordResetInput(string Token, string Password);

        /// <summary>
        /// Performs a password reset for an existing user.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="input">Password reset payload.</param>
        [HttpPost("{id}/password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ExecutePasswordReset(
            [FromRoute] ObjectId id,
            UserExecutePasswordResetInput input
        ) {
            var user = await Mongo.GetUserById(id);
            if(user == null) {
                return NotFound();
            }

            if(user.PasswordResetToken != input.Token) {
                return NotFound();
            }

            if(!CheckPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            user.PasswordResetToken = null;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password);
            await Mongo.ReplaceUser(user);

            return Ok();
        }

        public record UserLoginInput(string Email, string Password, string ClientName, bool Persistent);

        public record UserLoginOutput(string Id, DateTime LoginUntil, bool Verified);

        /// <summary>
        /// Logs in as a user and creates a new session token.
        /// </summary>
        /// <param name="input">Login payload.</param>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Login(UserLoginInput input) {
            var user = await Mongo.GetUserByEmail(input.Email);
            if(user == null) {
                // Delay response to throttle
                await Task.Delay(1050);
                return NotFound();
            }

            if(!BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash)) {
                // Delay response to throttle
                await Task.Delay(1000);
                return NotFound();
            }

            var newSessionId = Guid.NewGuid();
            var sessionStart = DateTime.UtcNow;
            var sessionEnd = sessionStart.Add(Startup.DefaultCookieValidity);

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.Name),
                    new Claim(ClaimTypes.Surname, user.Surname),
                    new Claim(Startup.CookieSessionClaimType, newSessionId.ToString("N"))
                }, CookieAuthenticationDefaults.AuthenticationScheme)
            );
            var authProperties = new AuthenticationProperties {
                IssuedUtc = sessionStart,
                IsPersistent = input.Persistent,
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties
            );

            Logger.LogDebug("Login performed for user {0} with session ID {1}", user.Id, newSessionId);

            await Mongo.StoreSession(new Session {
                Id = newSessionId,
                UserId = user.Id,
                StartedOn = sessionStart,
                Client = input.ClientName,
                UserAgent = HttpContext.Request.Headers[HeaderNames.UserAgent]
            });

            return Ok(new UserLoginOutput(
                user.Id.ToString(),
                sessionEnd,
                user.VerificationToken == null
            ));
        }

        /// <summary>
        /// Logs out the currently logged in user and voids existing sessions.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok();
        }

    }

}
