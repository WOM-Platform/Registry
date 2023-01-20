using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WomPlatform.Web.Api {
    public class RequireHttpsInProd : Attribute, IAuthorizationFilter, IOrderedFilter {

        /// <inheritdoc />
        /// <value>Default is <c>int.MinValue + 50</c> to run this <see cref="IAuthorizationFilter"/> early.</value>
        public int Order { get; set; } = int.MinValue + 50;

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized. Confirms requests are received over
        /// HTTPS. Takes no action for HTTPS requests. Otherwise if it was a GET request, sets
        /// <see cref="AuthorizationFilterContext.Result"/> to a result which will redirect the client to the HTTPS
        /// version of the request URI. Otherwise, sets <see cref="AuthorizationFilterContext.Result"/> to a result
        /// which will set the status code to <c>403</c> (Forbidden).
        /// </summary>
        /// <inheritdoc />
        public virtual void OnAuthorization(AuthorizationFilterContext filterContext) {
            if(filterContext == null) {
                throw new ArgumentNullException(nameof(filterContext));
            }

            var env = filterContext.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if(!env.IsDevelopment() && !filterContext.HttpContext.Request.IsHttps) {
                filterContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
        }
    }
}
