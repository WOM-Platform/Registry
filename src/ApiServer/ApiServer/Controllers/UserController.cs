using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v{version:apiVersion}/user")]
    [ApiVersion("1.0")]
    [RequireHttps]
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

        private bool CheckPassword(string password) {
            if(string.IsNullOrWhiteSpace(password)) {
                return false;
            }

            if(password.Length < 8) {
                return false;
            }

            return true;
        }

        /*
        [TempData]
        public bool PreviousLoginFailed { get; set; } = false;

        [TempData]
        public bool HasResetPassword { get; set; } = false;

        [HttpGet("login")]
        public IActionResult Login(
            [FromQuery] string @return
        ) {
            return View("Login", new LoginViewModel {
                PreviousLoginFailed = PreviousLoginFailed,
                HasResetPassword = HasResetPassword,
                ReturnUrl = @return
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginPerform(
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string @return
        ) {
            Logger.LogDebug("Login attempt by email {0}", email);

            var user = await Mongo.GetUserByEmail(email);
            if(user == null) {
                PreviousLoginFailed = true;
                return RedirectToAction(nameof(Login), new {
                    @return
                });
            }

            if(!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) {
                PreviousLoginFailed = true;
                return RedirectToAction(nameof(Login), new {
                    @return
                });
            }

            if(user.VerificationToken != null) {
                Logger.LogInformation("User {0} logging in but not verified", user.Id);
                return RedirectToAction(nameof(UserController.WaitForVerification), "User");
            }

            Logger.LogInformation("User {0} logged in", user.Id);

            var activeMerchant = (await Mongo.GetMerchantsWithAdminControl(user.Id)).FirstOrDefault();
            Logger.LogDebug("User {0} selecting merchant {1} as active", user.Id, activeMerchant?.Id);

            await InternalLogin(user, activeMerchant);

            if(@return != null) {
                return LocalRedirect(@return);
            }
            else {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
        */

        public record RegisterInput(
            string Email, string Password, string Name, string Surname
        );

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(RegisterInput input) {
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
                        id = user.Id.ToString(),
                        version = "1"
                    },
                    new {
                        user.Id,
                        user.Email,
                        user.Name,
                        user.Surname
                    }
                );
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to register new user with email {0}", input.Email);
                throw;
            }
        }

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

            return Ok(new {
                existingUser.Id,
                existingUser.Email,
                existingUser.Name,
                existingUser.Surname
            });
        }

        public record VerifyInput(string Token);

        [HttpPost("{id}/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Verify(
            [FromRoute] ObjectId id,
            VerifyInput input
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

        public record RequestPasswordResetInput(string Email);

        [HttpPost("password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestPasswordReset(
            RequestPasswordResetInput input
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

        public record ExecutePasswordResetInput(string Token, string Password);

        [HttpPost("{id}/password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ExecutePasswordReset(
            [FromRoute] ObjectId id,
            ExecutePasswordResetInput input
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

        public record LoginInput(string Email, string Password);

        public record LoginOutput(string Id, string Token, DateTime LoginUntil);

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Login(LoginInput input) {
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

            var securityHandler = new JwtSecurityTokenHandler();
            var jwtKey = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_USER_TOKEN_SECRET"));
            var jwtJti = Guid.NewGuid().ToString("N");
            var jwtDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.Name),
                    new Claim(ClaimTypes.Surname, user.Surname),
                    new Claim(JwtRegisteredClaimNames.Jti, jwtJti)
                }),
                Issuer = Startup.GetJwtIssuerName(),
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddYears(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(jwtKey),
                    SecurityAlgorithms.HmacSha512Signature
                )
            };
            var token = securityHandler.CreateToken(jwtDescriptor);

            Logger.LogDebug("Login performed for user {0} until {1}", user.Id, token.ValidTo);

            return Ok(new LoginOutput(
                user.Id.ToString(),
                securityHandler.WriteToken(token),
                token.ValidTo
            ));
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout() {
            return Ok();
        }

        /*
        [HttpGet("register-merchant")]
        public IActionResult RegisterMerchant() {
            if(HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier) != null) {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return View("MerchantRegister");
        }

        [HttpPost("register-merchant")]
        public async Task<IActionResult> RegisterMerchantPerform(
            [FromForm] UserRegisterMerchantModel inputMerchant,
            [FromForm] UserRegisterPosModelOptional inputPos
        ) {
            if(!ModelState.IsValid) {
                return View("MerchantRegister");
            }

            var existingUser = await Mongo.GetUserByEmail(inputMerchant.Email);
            if(existingUser != null) {
                ModelState.AddModelError(nameof(inputMerchant.Email), "Email already registered");
                return View("MerchantRegister");
            }

            var existingMerchant = await Mongo.GetMerchantByFiscalCode(inputMerchant.MerchantFiscalCode);
            if(existingMerchant != null) {
                ModelState.AddModelError(nameof(inputMerchant.MerchantFiscalCode), "Fiscal code already registered");
                return View("MerchantRegister");
            }

            var verificationToken = new Random().GenerateReadableCode(8);
            Logger.LogDebug("Registering new user for {0} with verification token {1}", inputMerchant.Email, verificationToken);

            try {
                var docUser = new User {
                    Email = inputMerchant.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(inputMerchant.Password),
                    Name = inputMerchant.Name,
                    Surname = inputMerchant.Surname,
                    VerificationToken = verificationToken,
                    RegisteredOn = DateTime.UtcNow
                };
                await Mongo.CreateUser(docUser);

                var docMerchant = new Merchant {
                    Name = inputMerchant.MerchantTitle,
                    FiscalCode = inputMerchant.MerchantFiscalCode,
                    PrimaryActivityType = inputMerchant.MerchantActivityType,
                    Address = inputMerchant.MerchantAddress,
                    ZipCode = inputMerchant.MerchantZipCode,
                    City = inputMerchant.MerchantCity,
                    Country = inputMerchant.MerchantNation,
                    Description = inputMerchant.MerchantDescription,
                    WebsiteUrl = inputMerchant.MerchantWebsite,
                    CreatedOn = DateTime.UtcNow,
                    AdministratorIds = new ObjectId[] {
                        docUser.Id
                    }
                };
                await Mongo.CreateMerchant(docMerchant);

                if(inputPos.IsSet()) {
                    Logger.LogInformation("POS model is valid, creating new POS");
                    await CreatePos(docMerchant.Id, inputPos.PosName, inputPos.PosUrl, inputPos.PosLatitude.Value, inputPos.PosLongitude.Value);
                }
                else {
                    Logger.LogDebug("POS model not valid, ignoring");
                }

                _composer.SendVerificationMail(docUser);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to register");
                ModelState.AddModelError("Internal", "Failed to register, try again later");
                return View("MerchantRegister");
            }

            return RedirectToAction(nameof(WaitForVerification));
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [Authorize(Startup.UserLoginPolicy)]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile() {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await Mongo.GetUserById(new ObjectId(userId));

            return View("Profile", new UserProfileModel {
                Email = user.Email,
                Name = user.Name,
                Surname = user.Surname
            });
        }

        [Authorize(Startup.UserLoginPolicy)]
        [HttpPost("profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromForm] UserProfileModel user
        ) {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            await Mongo.UpdateUser(new ObjectId(userId), name: user.Name, surname: user.Surname);

            return RedirectToAction(nameof(Profile));
        }

        private Task InternalLogin(User userProfile, Merchant activeMerchant) {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, $"{userProfile.Name} {userProfile.Surname}"),
                new Claim(ClaimTypes.NameIdentifier, userProfile.Id.ToString()),
                new Claim(ClaimTypes.GivenName, userProfile.Name),
                new Claim(ClaimTypes.Email, userProfile.Email)
            };
            if(activeMerchant != null) {
                claims.Add(new Claim(Startup.ActiveMerchantClaimType, activeMerchant.Id.ToString()));
            }

            return HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(
                    new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
                ),
                new AuthenticationProperties {
                    AllowRefresh = true,
                    IsPersistent = true
                }
            );
        }

        */

    }

}
