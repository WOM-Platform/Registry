using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("user")]
    public class UserController : Controller {

        private readonly MongoDatabase _mongo;
        private readonly MailComposer _composer;
        private readonly ILogger<UserController> _logger;

        public UserController(
            MongoDatabase mongo,
            MailComposer composer,
            ILogger<UserController> logger
        ) {
            _mongo = mongo;
            _composer = composer;
            _logger = logger;
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
            _logger.LogDebug("Login attempt by email {0}", email);

            var user = await _mongo.GetUserByEmail(email);
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
                _logger.LogInformation("User {0} logging in but not verified");
                return RedirectToAction(nameof(UserController.WaitForVerification), "User");
            }

            _logger.LogInformation("User {0} logged in", email);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(
                    new ClaimsIdentity(new Claim[] {
                        new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.GivenName, user.Name),
                        new Claim(ClaimTypes.Email, user.Email)
                    }, CookieAuthenticationDefaults.AuthenticationScheme)
                ),
                new AuthenticationProperties {
                    AllowRefresh = true,
                    IsPersistent = true
                }
            );

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
        public async Task<IActionResult> PerformRegisterMerchant(
            [FromForm] UserRegisterMerchantModel input
        ) {
            if(!ModelState.IsValid) {
                return View("MerchantRegister");
            }

            try {
                var docUser = new User {
                    Email = input.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                    Name = input.Name,
                    Surname = input.Surname,
                    VerificationToken = new Random().GenerateReadableCode(8)
                };
                await _mongo.CreateUser(docUser);

                await _mongo.CreateMerchant(new Merchant {
                    Name = input.MerchantTitle,
                    FiscalCode = input.MerchantFiscalCode,
                    PrimaryActivityType = input.MerchantActivityType,
                    Address = input.MerchantAddress,
                    ZipCode = input.MerchantZipCode,
                    City = input.MerchantCity,
                    Nation = input.MerchantNation,
                    Description = input.MerchantDescription,
                    WebsiteUrl = input.MerchantWebsite,
                    AdministratorUserIds = new string[] {
                        docUser.Id
                    }
                });

                _composer.SendVerificationMail(docUser);
            }
            catch(Exception ex) {
                _logger.LogError(ex, "Failed to register");
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
            var user = await _mongo.GetUserById(userId);
            if(user == null) {
                return NotFound();
            }

            if(user.VerificationToken != token) {
                return NotFound();
            }

            user.VerificationToken = null;
            await _mongo.ReplaceUser(user);

            return View("Verified");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet("profile")]
        public IActionResult Profile() {
            return Content("Profile");
        }

    }

}
