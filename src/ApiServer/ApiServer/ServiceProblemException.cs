using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api {
    public class ServiceProblemException : Exception {

        public ServiceProblemException(
            string type,
            string title,
            int statusCode = StatusCodes.Status500InternalServerError,
            string detail = null,
            params string[] extensionPairs
        ) {
            Type = type;
            Title = title;
            HttpStatus = statusCode;
            Detail = detail;
            if(extensionPairs != null) {
                if(extensionPairs.Length % 2 != 0) {
                    throw new ArgumentException("List of extension pairs must be even");
                }

                Extensions = new Dictionary<string, object>();
                for(int i = 0; i < extensionPairs.Length; i += 2) {
                    Extensions.Add(extensionPairs[i], extensionPairs[i + 1]);
                }
            }
        }

        public ServiceProblemException(
            string type,
            string title,
            int statusCode = StatusCodes.Status500InternalServerError,
            string detail = null,
            Dictionary<string, string> extensions = null
        ) {
            Type = type;
            Title = title;
            HttpStatus = statusCode;
            Detail = detail;
            if(extensions != null) {
                Extensions = new Dictionary<string, object>();
                foreach(var pair in extensions) {
                    Extensions.Add(pair.Key, pair.Value);
                }
            }
        }

        public string Type { get; init; }

        public string Title { get; init; }

        public int HttpStatus { get; init; }

        public string? Detail { get; init; }

        public IDictionary<string, object?> Extensions { get; init; }

        public ActionResult ToActionResult() {
            var details = new ProblemDetails {
                Type = Type,
                Title = Title,
                Status = HttpStatus,
                Detail = Detail
            };
            if(Extensions != null) {
                foreach(var pair in Extensions) {
                    details.Extensions.Add(pair.Key, pair.Value);
                }
            }

            return new ObjectResult(details) {
                StatusCode = HttpStatus,
            };
        }

    }
}
