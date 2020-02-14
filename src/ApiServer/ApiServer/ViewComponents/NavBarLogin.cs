using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.ViewComponents {

    public class NavBarLogin : ViewComponent {

        private LoginStatusViewModel GetLoggedInUser() {
            if(HttpContext.User == null) {
                return new LoginStatusViewModel {
                    IsLoggedIn = false
                };
            }

            if(!HttpContext.User.Identity.IsAuthenticated) {
                return new LoginStatusViewModel {
                    IsLoggedIn = false
                };
            }

            return new LoginStatusViewModel {
                IsLoggedIn = true,
                Username = HttpContext.User.Identity.Name
            };
        }

        public IViewComponentResult Invoke() {
            return View(GetLoggedInUser());
        }

    }

}
