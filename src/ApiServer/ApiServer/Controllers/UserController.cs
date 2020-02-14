using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("user/{action:slugify}")]
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

        public IActionResult Login(
            [FromQuery] string @return
        ) {
            return View("Login", new LoginViewModel {
                PreviousLoginFailed = PreviousLoginFailed,
                ReturnUrl = @return
            });
        }

        [HttpPost]
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

        public IActionResult Register() {
            return Content("Register");
        }

        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

    }

}
