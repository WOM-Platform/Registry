using System;
using System.ComponentModel.DataAnnotations;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Merchant;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.OutputModels.User;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/user")]
    [OperationsTags("User and session management")]
    [RequireHttpsInProd]
    public class UserController : BaseRegistryController {

        public UserController(
            IServiceProvider serviceProvider,
            ILogger<UserController> logger)
        : base(serviceProvider, logger) {
        }

        /// <summary>
        /// User registration payload.
        /// </summary>
        public record UserRegisterInput(
            string Email, string Password, string Name, string Surname, bool Verified = false, PlatformRole Role = PlatformRole.User
        );

        /// <summary>
        /// Register a new user to the service.
        /// </summary>
        /// <param name="input">User registration payload.</param>
        [HttpPost]
        [Authorize]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserCreationOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(UserRegisterInput input) {
            if(!CheckUserPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }
            if(input.Verified || input.Role > PlatformRole.User) {
                await this.VerifyUserIsAdmin();
            }

            Logger.LogInformation("Registering new user for {0} and role {1}", input.Email, input.Role);

            try {
                var session = await CreateMongoSession();
                var user = await session.WithTransactionAsync(async (session, token) => {
                    var user = await UserService.CreateUser(session, input.Email, input.Name, input.Surname, input.Password, input.Verified, input.Role);
                    Logger.LogInformation("New user {0} created for {1}", user.Id, user.Email);

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
                        Surname = user.Surname,
                        Verified = user.VerificationToken == null,
                    }
                );
            }
            catch(Exception) {
                Logger.LogError("Failed to register new user with email {0}", input.Email);
                throw;
            }
        }

        /// <summary>
        /// Register a new user to the service.
        /// </summary>
        /// <param name="input">User registration payload.</param>
        [HttpPost("register")]
        [Obsolete]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserCreationOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public Task<IActionResult> RegisterLegacy(UserRegisterInput input) {
            return Register(input);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> SearchUsers(
            [FromQuery] string name,
            [FromQuery] string email,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        ) {
            await VerifyUserIsAdmin();

            (var results, var count) = await UserService.Search(name, email, page, pageSize);

            return Ok(Paged<UserOutput>.FromPage(
                (from u in results select u.ToOutput(false)).ToArray(),
                page,
                pageSize,
                count
            ));
        }

        /// <summary>
        /// Retrieves detailed information about the currently logged-in user.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDetailedOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetOwnInformation() {
            if(!User.GetUserId(out var myself)) {
                return Forbid();
            }

            var user = await UserService.GetUserById(myself);
            if(user == null) {
                return NotFound();
            }

            var aims = AimService.GetAllAimCodes();
            var taskSources = SourceService.GetSourcesByUser(myself);
            var taskMerchants = PosService.GetMerchantsAndPosByUser(myself);
            await Task.WhenAll(taskSources, taskMerchants);

            return Ok(new UserDetailedOutput {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Surname = user.Surname,
                Verified = user.VerificationToken == null,
                Role = user.Role,
                Merchants = (from m in taskMerchants.Result.Keys
                             let pos = taskMerchants.Result[m]
                             orderby m.Id
                             select m.ToAuthOutput(
                                 (from p in pos
                                  let pictureOutput = PicturesService.GetPosCoverOutput(p.CoverPath, p.CoverBlurHash)
                                  select p.ToAuthOutput(pictureOutput)).ToArray(),
                                 m.Access.Get(myself).Role
                             )).ToArray(),
                Sources = (from s in taskSources.Result
                           let customGeneratorPic = PicturesService.GetPictureOutput(s.CustomGenerator?.LogoPath, s.CustomGenerator?.LogoBlurHash)
                           select new SourceAuthDetailsOutput(s, aims, customGeneratorPic)).ToArray(),
            });
        }

        /// <summary>
        /// Retrieves information about an existing user.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            if(!User.GetUserId(out var myself)) {
                return Forbid();
            }

            var user = await UserService.GetUserById(id);
            if(user == null) {
                return NotFound();
            }

            return Ok(user.ToOutput(id != myself));
        }

        public record UserUpdateInformationInput(string Email, string Name, string Surname, string Password);

        /// <summary>
        /// Updates information about an existing user.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="input">User information payload.</param>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(UserOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(void), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateInformation(
            [FromRoute] ObjectId id,
            [FromBody] UserUpdateInformationInput input
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
                var updatedUser = await UserService.UpdateUser(id, name: input.Name, surname: input.Surname, password: input.Password);

                return Ok(updatedUser.ToOutput(false));
            }
            catch(Exception) {
                Logger.LogError("Failed to update user {0}", id);
                throw;
            }
        }

        /// <summary>
        /// Requests a user verification e-mail.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpPost("{id}/request-verification")]
        [Obsolete]
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

            UserService.RequestVerificationEmail(user);

            return Ok();
        }

        public record RequestVerificationEmail(string Email);

        /// <summary>
        /// Unauthenticated requests for a new user verification e-mail.
        /// </summary>
        [HttpPost("request-verification")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestVerificationByEmail(
            [FromBody] RequestVerificationEmail payload
        ) {
            var user = await UserService.GetUserByEmail(payload?.Email);
            if(user == null) {
                return Ok();
            }

            UserService.RequestVerificationEmail(user);

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
        [Obsolete]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> VerifyLegacy(
            [FromRoute] ObjectId id,
            [FromQuery] UserVerifyInput input
        ) {
            await UserService.PerformVerification(id, input.Token);

            return Ok();
        }

        public record UserEmailVerifyInput([Required][EmailAddress] string Email, [Required] string Token);

        /// <summary>
        /// Verifies a user account.
        /// </summary>
        /// <remarks>
        /// Must be authenticated as the same user that is being verified.
        /// </remarks>
        /// <param name="input">User verification payload.</param>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Verify(
            [FromBody][Required] UserEmailVerifyInput input
        ) {
            var user = await UserService.GetUserByEmail(input.Email);
            if(user == null) {
                return NotFound();
            }
            if(user.VerificationToken == null) {
                return Ok();
            }

            await UserService.PerformVerification(user.Id, input.Token);

            return Ok();
        }

        #region Password reset process

        public record UserRequestPasswordResetInput([Required] string Email);

        /// <summary>
        /// Requests a password reset for an existing user.
        /// </summary>
        /// <param name="input">Password request payload.</param>
        [HttpPost("password-reset")]
        [Obsolete]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestPasswordResetLegacy(
            [Required] UserRequestPasswordResetInput input
        ) {
            try {
                await UserService.RequestPasswordReset(input.Email);
            }
            catch(ServiceProblemException ex) when(ex.Type == ServiceProblemException.UserNotFoundType) {
                // Ignore error to prevent data leakage to user
            }

            return Ok();
        }

        /// <summary>
        /// Requests a password reset for an existing user.
        /// </summary>
        /// <param name="input">Password request payload.</param>
        [HttpPost("password/reset")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestPasswordReset(
            [Required] UserRequestPasswordResetInput input
        ) {
            try {
                await UserService.RequestPasswordReset(input.Email);
            }
            catch(ServiceProblemException ex) when(ex.Type == ServiceProblemException.UserNotFoundType) {
                // Ignore error to prevent data leakage to user
            }

            return Ok();
        }

        public record UserEmailExecutePasswordResetInput([Required][EmailAddress] string Email, [Required] string Token, [Required] string Password);

        /// <summary>
        /// Performs a password reset for an existing user.
        /// </summary>
        /// <param name="input">Password reset payload.</param>
        [HttpPut("password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ExecutePasswordReset(
            [FromBody][Required] UserEmailExecutePasswordResetInput input
        ) {
            var user = await UserService.GetUserByEmail(input.Email);
            if(user == null) {
                return NotFound();
            }

            if(!CheckUserPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            await UserService.PerformPasswordReset(user.Id, input.Token, input.Password);

            return Ok();
        }

        public record UserIdExecutePasswordResetInput([Required] string Token, [Required] string Password);

        /// <summary>
        /// Performs a password reset for an existing user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="input">Password reset payload.</param>
        [HttpPost("{userId}/password-reset")]
        [Obsolete]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ExecutePasswordResetLegacy(
            [FromRoute] ObjectId userId,
            [FromBody][Required] UserIdExecutePasswordResetInput input
        ) {
            if(!CheckUserPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            await UserService.PerformPasswordReset(userId, input.Token, input.Password);

            return Ok();
        }

        /// <summary>
        /// Performs a password reset for an existing user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="input">Password reset payload.</param>
        [HttpPut("{userId}/password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ExecutePasswordReset(
            [FromRoute] ObjectId userId,
            [FromBody][Required] UserIdExecutePasswordResetInput input
        ) {
            if(!CheckUserPassword(input.Password)) {
                return this.ProblemParameter("Password is not secure");
            }

            await UserService.PerformPasswordReset(userId, input.Token, input.Password);

            return Ok();
        }

        #endregion

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
                await Task.Delay(1000);
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
            var securitySection = Configuration.GetRequiredSection("Security");

            var user = await GetUserToLogin(input);
            if(user == null) {
                return NotFound();
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var issueTimestamp = DateTime.UtcNow;
            var expirationTimestamp = issueTimestamp.AddDays(1);

            var securityHandler = new JwtSecurityTokenHandler();
            var jwtKey = Encoding.UTF8.GetBytes(securitySection["JwtTokenSigningKey"]);
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
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Only user can delete their own user profile");
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
