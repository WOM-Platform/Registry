using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Used to constrain an action based on the request's Accept header.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    class AcceptHeaderAttribute : Attribute, IActionConstraint {

        public AcceptHeaderAttribute(string contentType) {
            if(contentType == null) {
                throw new ArgumentNullException(nameof(contentType));
            }

            ContentTypes = new MediaTypeCollection {
                MediaTypeHeaderValue.Parse(contentType)
            };
        }

        public MediaTypeCollection ContentTypes {
            get; private set;
        }

        public int Order {
            get {
                return 0;
            }
        }

        private bool IsSubsetOfAnyContentType(string requestMediaType) {
            var parsedRequestMediaType = new MediaType(requestMediaType);
            for(var i = 0; i < ContentTypes.Count; i++) {
                var contentTypeMediaType = new MediaType(ContentTypes[i]);
                if(parsedRequestMediaType.IsSubsetOf(contentTypeMediaType)) {
                    return true;
                }
            }
            return false;
        }

        public bool Accept(ActionConstraintContext context) {
            var requestAccept = context.RouteContext.HttpContext.Request.Headers[HeaderNames.Accept];
            if(string.IsNullOrEmpty(requestAccept)) {
                return true;
            }

            if(IsSubsetOfAnyContentType(requestAccept)) {
                return true;
            }

            return false;
        }

    }

}
