using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.ViewComponents {

    public class NavBarLogin : ViewComponent {

        private LoginViewModel GetLoggedInUser() {
            if(HttpContext.User == null) {
                return new LoginViewModel {
                    IsLoggedIn = false
                };
            }

            if(!HttpContext.User.Identity.IsAuthenticated) {
                return new LoginViewModel {
                    IsLoggedIn = false
                };
            }

            return new LoginViewModel {
                IsLoggedIn = true,
                Username = HttpContext.User.Identity.Name
            };
        }

        public IViewComponentResult Invoke() {
            return View(GetLoggedInUser());
        }

    }

}
