using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.OutputModels.User;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/user")]
    [RequireHttpsInProd]
    [OperationsTags("User and session management")]
    public class UserController : BaseRegistryController {

        private readonly MailComposer _composer;

        public UserController(
            MailComposer composer,
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
            _composer = composer;
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
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserCreationOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(UserRegisterInput input) {
            if(!CheckUserPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            Logger.LogDebug("Registering new user for {0}", input.Email);

            try {
                var session = await CreateMongoSession();
                var user = await session.WithTransactionAsync(async (session, token) => {
                    var user = await UserService.CreateUser(session, input.Email, input.Name, input.Surname, input.Password);
                    Logger.LogInformation("New user {0} created for {1}", user.Id, user.Email);

                    _composer.SendVerificationMail(user);

                    return user;
                });

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        id = user.Id.ToString()
                    },
                    new UserCreationOutput {
                        Id = user.Id,
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
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDetailedOutput), StatusCodes.Status200OK)]
        public Task<ActionResult> GetOwnInformation() {
            if(!User.GetUserId(out var myself)) {
                return Task.FromResult<ActionResult>(Forbid());
            }

            return GetInformation(myself);
        }

        /// <summary>
        /// Retrieves information about an existing user.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(UserDetailedOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            if(!User.UserIdEquals(id)) {
                return Forbid();
            }

            var user = await UserService.GetUserById(id);
            if(user == null) {
                return NotFound();
            }

            var taskAims = AimService.GetRootAimCodes();
            var taskSources = SourceService.GetSourcesByUser(id);
            var taskMerchants = PosService.GetMerchantsAndPosByUser(id);
            await Task.WhenAll(taskAims, taskSources, taskMerchants);

            return Ok(new UserDetailedOutput {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Surname = user.Surname,
                Verified = user.VerificationToken == null,
                Merchants = taskMerchants.Result.Select(d => new MerchantAuthOutput {
                    Id = d.Item1.Id.ToString(),
                    Name = d.Item1.Name,
                    FiscalCode = d.Item1.FiscalCode,
                    PrimaryActivity = d.Item1.PrimaryActivityType,
                    Address = d.Item1.Address,
                    ZipCode = d.Item1.ZipCode,
                    City = d.Item1.City,
                    Country = d.Item1.Country,
                    Description = d.Item1.Description,
                    Url = d.Item1.WebsiteUrl,
                    Pos = (from p in d.Item2
                           let pictureOutput = PicturesService.GetPosCoverOutput(p.CoverPath, p.CoverBlurHash)
                           select p.ToAuthOutput(pictureOutput)).ToArray(),
                    Enabled = d.Item1.Enabled,
                    Access = d.Item1.Access.Get(id).Role,
                }).ToArray(),
                Sources = taskSources.Result.Select(s => s.ToLoginV2Output(
                    cg => cg?.ToOutput(PicturesService.GetPictureOutput(cg.LogoPath, cg.LogoBlurHash)),
                    taskAims.Result
                )).ToArray()
            });
        }

        public record UserUpdateInformationInput(string Email, string Name, string Surname, string Password);

        /// <summary>
        /// Updates information about an existing user.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="input">User information payload.</param>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateInformation(
            [FromRoute] ObjectId id,
            UserUpdateInformationInput input
        ) {
            if(!User.UserIdEquals(id)) {
                return Forbid();
            }

            var existingUser = await UserService.GetUserById(id);
            if(existingUser == null) {
                return NotFound();
            }

            if(input.Password != null && !CheckUserPassword(input.Password)) {
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

                await UserService.ReplaceUser(existingUser);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update user {0}", id);
                throw;
            }

            // TODO: should return user information
            return Ok();
        }

        /// <summary>
        /// Requests a user verification e-mail.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpPost("{id}/request-verification")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestVerification(
            [FromRoute] ObjectId id
        ) {
            if(!User.UserIdEquals(id)) {
                return Forbid();
            }

            var user = await UserService.GetUserById(id);
            if(user == null) {
                return NotFound();
            }

            if(user.VerificationToken != null) {
                _composer.SendVerificationMail(user);
            }

            return Ok();
        }

        public record RequestVerificationEmail(string Email);

        /// <summary>
        /// Unauthenticated requests for a new user verification e-mail.
        /// </summary>
        [HttpPost("request-verification")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestVerificationByEmail(
            [FromBody] RequestVerificationEmail payload
        ) {
            var user = await UserService.GetUserByEmail(payload.Email);
            if(user == null) {
                return Ok();
            }

            if(user.VerificationToken != null) {
                _composer.SendVerificationMail(user);
            }

            return Ok();
        }

        public record UserVerifyInput(string Token);

        /// <summary>
        /// Verifies a user account.
        /// </summary>
        /// <remarks>
        /// Must be authenticated as the same user that is being verified.
        /// </remarks>
        /// <param name="id">User ID.</param>
        /// <param name="input">User verification payload.</param>
        [HttpPost("{id}/verify")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Verify(
            [FromRoute] ObjectId id,
            [FromQuery] UserVerifyInput input
        ) {
            var user = await UserService.GetUserById(id);
            if(user == null) {
                return NotFound();
            }

            if(user.VerificationToken == null) {
                return Ok();
            }

            if(user.VerificationToken != input.Token) {
                return this.ProblemParameter("Token not valid");
            }

            user.VerificationToken = null;
            await UserService.ReplaceUser(user);

            return Ok();
        }

        public record UserRequestPasswordResetInput(string Email);

        /// <summary>
        /// Requests a password reset for an existing user.
        /// </summary>
        /// <param name="input">Password request payload.</param>
        [HttpPost("password-reset")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestPasswordReset(
            UserRequestPasswordResetInput input
        ) {
            var user = await UserService.GetUserByEmail(input.Email);
            if(user != null) {
                if(user.PasswordResetToken == null) {
                    user.PasswordResetToken = new Random().GenerateReadableCode(8);
                    await UserService.ReplaceUser(user);
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
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ExecutePasswordReset(
            [FromRoute] ObjectId id,
            UserExecutePasswordResetInput input
        ) {
            var user = await UserService.GetUserById(id);
            if(user == null) {
                return NotFound();
            }

            if(user.PasswordResetToken != input.Token) {
                return NotFound();
            }

            if(!CheckUserPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            user.PasswordResetToken = null;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password);
            await UserService.ReplaceUser(user);

            return Ok();
        }

        public record UserLoginInput(string Email, string Password, string ClientName);

        public record UserLoginOutput(string Id, string Token, DateTime LoginUntil, bool Verified);

        /// <summary>
        /// Gets the user to login, either through login data or through the already authenticated basic authentication.
        /// </summary>
        private async Task<User> GetUserToLogin(UserLoginInput input) {
            if(User.GetUserId(out var loggedInUser)) {
                Logger.LogDebug("User {0} already logged in", loggedInUser);
                return await UserService.GetUserById(loggedInUser);
            }

            if(input == null) {
                return null;
            }

            var user = await UserService.GetUserByEmail(input.Email);
            if(user == null) {
                Logger.LogTrace("User {0} does not exist", input.Email);

                // Delay response to throttle
                await Task.Delay(1050);
                return null;
            }

            if(!BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash)) {
                Logger.LogTrace("User {0} password not correct", input.Email);

                // Delay response to throttle
                await Task.Delay(1000);
                return null;
            }

            return user;
        }

        /// <summary>
        /// Logs in as a user and creates a new session token.
        /// </summary>
        /// <param name="input">Login payload.</param>
        [HttpPost("login")]
        [AllowAnonymous]
        [ForceAuthChallenge(BasicAuthenticationSchemeOptions.SchemeName)]
        [ProducesResponseType(typeof(UserLoginOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Login(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UserLoginInput input
        ) {
            var user = await GetUserToLogin(input);
            if(user == null) {
                return NotFound();
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var issueTimestamp = DateTime.UtcNow;
            var expirationTimestamp = issueTimestamp.AddDays(1);

            var securityHandler = new JwtSecurityTokenHandler();
            var jwtKey = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_USER_TOKEN_SECRET"));
            var jwtDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.Name),
                    new Claim(ClaimTypes.Surname, user.Surname),
                    new Claim(JwtRegisteredClaimNames.Jti, sessionId)
                }),
                Issuer = Startup.GetJwtIssuerName(),
                IssuedAt = issueTimestamp,
                Expires = expirationTimestamp,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(jwtKey),
                    SecurityAlgorithms.HmacSha512Signature
                )
            };

            var token = securityHandler.CreateToken(jwtDescriptor);

            Logger.LogDebug("Login performed for user {0} with session ID {1}", user.Id, sessionId);

            return Ok(new UserLoginOutput(
                user.Id.ToString(),
                securityHandler.WriteToken(token),
                expirationTimestamp,
                user.VerificationToken == null
            ));
        }

        /// <summary>
        /// Deletes a user profile.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(
            [FromRoute] ObjectId id
        ) {
            if(!User.UserIdEquals(id)) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Only user can delete own user profile");
            }

            var existingUser = await UserService.GetUserById(id);
            if(existingUser == null) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "User does not exist");
            }

            var merchantsWithAdminAccess = await MerchantService.GetMerchantsWithAdminControl(existingUser.Id);
            if(merchantsWithAdminAccess.Count > 0) {
                Logger.LogWarning("Deleting user {0} with admin access to {1} merchants", existingUser.Id, merchantsWithAdminAccess.Count);
            }

            await UserService.DeleteUser(existingUser.Id);

            return Ok();
        }

    }

}
