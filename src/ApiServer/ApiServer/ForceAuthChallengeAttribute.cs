using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WomPlatform.Web.Api {

    public class ForceAuthChallengeAttribute : ActionFilterAttribute {

        private readonly string _authScheme;

        public ForceAuthChallengeAttribute(string authenticationScheme) {
            _authScheme = authenticationScheme;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var result = await context.HttpContext.AuthenticateAsync(_authScheme);
            if(result.Succeeded) {
                // Auth succeeded, combine all claims identities from auth and existing user
                var claims = new List<ClaimsIdentity>();
                claims.AddRange(result.Principal.Identities);
                if(context.HttpContext.User?.Identities != null) {
                    claims.AddRange(context.HttpContext.User.Identities);
                }

                // Set new claims principal on context
                var newPrincipal = new ClaimsPrincipal(claims);
                context.HttpContext.User = newPrincipal;
            }

            await next.Invoke();
        }

    }

}
