using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api {

    public class XForwardedProtoAttribute : ActionFilterAttribute {

        private readonly string _forwardedProtoScheme;

        public XForwardedProtoAttribute(string forwardedProtoScheme) {
            _forwardedProtoScheme = forwardedProtoScheme;
        }

        public override void OnActionExecuting(ActionExecutingContext context) {
            var request = context.HttpContext.Request;

            if(request.Headers.TryGetValue("X-Forwarded-Proto", out var forwardedProtoHeader)) {
                if(forwardedProtoHeader.Count < 1 || !forwardedProtoHeader[0].Equals(_forwardedProtoScheme, StringComparison.InvariantCultureIgnoreCase)) {
                    context.Result = new StatusCodeResult(426);
                }
            }
            else {
                context.Result = new StatusCodeResult(426);
            }
        }

    }

}
