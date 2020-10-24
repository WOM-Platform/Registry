using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("user")]
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

        [TempData]
        public bool PreviousLoginFailed { get; set; } = false;

        [HttpGet("login")]
        public IActionResult Login(
            [FromQuery] string @return
        ) {
            return View("Login", new LoginViewModel {
                PreviousLoginFailed = PreviousLoginFailed,
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

        [HttpGet("verify")]
        public IActionResult WaitForVerification(
        ) {
            return View("Wait");
        }

        [HttpGet("verify/{userId}/{token}")]
        public async Task<IActionResult> Verify(
            [FromRoute] string userId,
            [FromRoute] string token
        ) {
            var user = await Mongo.GetUserById(new ObjectId(userId));
            if(user == null) {
                return NotFound();
            }

            if(user.VerificationToken != token) {
                return NotFound();
            }

            return View("Verification", new LoginVerificationViewModel {
                UserId = userId,
                Token = token
            });
        }

        [HttpPost("verify/{userId}/{token}")]
        public async Task<IActionResult> VerifyPerform(
            [FromRoute] string userId,
            [FromRoute] string token
        ) {
            var user = await Mongo.GetUserById(new ObjectId(userId));
            if(user == null) {
                return NotFound();
            }

            if(user.VerificationToken != token) {
                return NotFound();
            }

            user.VerificationToken = null;
            await Mongo.ReplaceUser(user);

            var activeMerchant = (await Mongo.GetMerchantsWithAdminControl(user.Id)).FirstOrDefault();
            Logger.LogDebug("User {0} selecting merchant {1} as active", user.Id, activeMerchant?.Id);

            await InternalLogin(user, activeMerchant);

            return RedirectToAction(nameof(HomeController.Index), "Home");
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

    }

}
