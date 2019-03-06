using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace WomPlatform.Web.Api {

    public static class HttpExtensions {

        public static bool HasAcceptHeader(this HttpRequest request, string mediaType) {
            var acceptHeader = request.Headers[HeaderNames.Accept];
            if(string.IsNullOrWhiteSpace(acceptHeader)) {
                return false;
            }

            var parsedRequestMediaType = new MediaType(acceptHeader);
            if(parsedRequestMediaType.IsSubsetOf(new MediaType(mediaType))) {
                return true;
            }

            return false;
        }

    }

}
