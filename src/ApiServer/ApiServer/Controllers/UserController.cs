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

    [Route("user/[action]")]
    public class UserController : Controller {

        private readonly MongoDatabase _mongo;
        private readonly ILogger<UserController> _logger;

        public UserController(
            MongoDatabase mongo,
            ILogger<UserController> logger
        ) {
            _mongo = mongo;
            _logger = logger;
        }

        [TempData]
        public bool PreviousLoginFailed { get; set; } = false;

        [ActionName("login")]
        public IActionResult Login(
            [FromQuery] string @return
        ) {
            return View("Login", new LoginViewModel {
                PreviousLoginFailed = PreviousLoginFailed,
                ReturnUrl = @return
            });
        }

        [HttpPost]
        [ActionName("perform-login")]
        public async Task<IActionResult> PerformLogin(
            [FromForm] string username,
            [FromForm] string password,
            [FromForm] string @return
        ) {
            _logger.LogDebug("Login attempt by user {0}", username);

            var user = await _mongo.GetUserByUsername(username);
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

            _logger.LogInformation("User {0} logged in", username);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(
                    new ClaimsIdentity(new Claim[] {
                        new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
                        new Claim(ClaimTypes.NameIdentifier, username),
                        new Claim(ClaimTypes.GivenName, user.Name)
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

        [HttpGet]
        [ActionName("register")]
        public IActionResult Register() {
            if(HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier) != null) {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return View("Register");
        }

        [HttpPost]
        [ActionName("register")]
        public async Task<IActionResult> PerformRegister(
            [FromForm] UserRegisterModel user
        ) {
            if(!ModelState.IsValid) {
                return View("Register");
            }

            try {
                await _mongo.CreateUser(new User {
                    Username = user.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                    Emails = new string[] {
                    user.Email
                },
                    FiscalCode = user.FiscalCode,
                    Name = user.Name,
                    Surname = user.Surname,
                    VerificationToken = new Random().GenerateReadableCode(8)
                });
            }
            catch(Exception ex) {
                _logger.LogError(ex, "Failed to create user");
                ModelState.AddModelError("Internal", "Failed to create user");
                return View("Register");
            }

            return RedirectToAction(nameof(WaitForVerification));
        }

        [HttpGet]
        [ActionName("wait-for-verification")]
        public IActionResult WaitForVerification(
        ) {
            return View("Wait");
        }

        [HttpGet("{userId}/{token}")]
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

        [HttpGet]
        [ActionName("logout")]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

    }

}
