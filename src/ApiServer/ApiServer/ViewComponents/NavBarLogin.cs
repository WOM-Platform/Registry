using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.ViewComponents {

    public class NavBarLogin : ViewComponent {

        private readonly ILogger<NavBarLogin> _logger;

        public NavBarLogin(
            ILogger<NavBarLogin> logger
        ) {
            _logger = logger;
        }

        private LoginStatusViewModel GetLoggedInUser() {
            if(HttpContext.User == null) {
                return new LoginStatusViewModel {
                    IsLoggedIn = false
                };
            }

            var claimNameId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var claimName = HttpContext.User.FindFirst(ClaimTypes.Name);
            if(claimNameId == null) {
                return new LoginStatusViewModel {
                    IsLoggedIn = false
                };
            }

            return new LoginStatusViewModel {
                IsLoggedIn = true,
                UserIdentifier = claimNameId.Value,
                FullName = claimName.Value
            };
        }

        public IViewComponentResult Invoke() {
            return View(GetLoggedInUser());
        }

    }

}
