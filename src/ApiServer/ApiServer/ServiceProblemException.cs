using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api {
    public class ServiceProblemException : Exception {

        public ServiceProblemException(
            string title,
            int statusCode = StatusCodes.Status500InternalServerError,
            string type = null,
            params string[] extensionPairs
        ) {
            Type = type;
            Title = title;
            HttpStatus = statusCode;
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
            string title,
            int statusCode = StatusCodes.Status500InternalServerError,
            string type = null,
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

        public string? Type { get; init; }

        public string Title { get; init; }

        public int HttpStatus { get; init; }

        public string? Detail { get; init; }

        public IDictionary<string, object?> Extensions { get; init; }

        public ActionResult ToActionResult() {
            return new ObjectResult(ToProblemDetails()) {
                StatusCode = HttpStatus,
            };
        }

        public ProblemDetails ToProblemDetails() {
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
            return details;
        }

        public readonly static ServiceProblemException UserIsNotLoggedIn = new(
            "User is not logged in",
            StatusCodes.Status401Unauthorized,
            type: "https://wom.social/api/problems/user-not-logged-in"
        );

        public readonly static ServiceProblemException EmailAlreadyRegistered = new(
            "Email already registered",
            StatusCodes.Status400BadRequest,
            type: "https://wom.social/api/problems/email-already-registered"
        );

        public readonly static ServiceProblemException UserProfileDoesNotExist = new(
            "User profile does not exist",
            StatusCodes.Status401Unauthorized,
            type: "https://wom.social/api/problems/user-profile-not-found"
        );

        public const string UserNotFoundType = "https://wom.social/api/problems/user-not-found";

        public readonly static ServiceProblemException UserNotFound = new(
            "User not found",
            StatusCodes.Status404NotFound,
            type: UserNotFoundType
        );

        public readonly static ServiceProblemException UserIsNotAdmin = new(
            "User is not administrator",
            StatusCodes.Status403Forbidden,
            type: "https://wom.social/api/problems/user-not-administrator"
        );

        public readonly static ServiceProblemException UserIsNotAdminOfSource = new(
            "User is not authorized as source admin",
            StatusCodes.Status403Forbidden,
            type: "https://wom.social/api/problems/user-not-administrator-of-source"
        );

        public readonly static ServiceProblemException UserIsNotUserOfMerchant = new(
            "User is not authorized as merchant user",
            StatusCodes.Status403Forbidden,
            type: "https://wom.social/api/problems/user-not-user-of-merchant"
        );

        public readonly static ServiceProblemException UserIsNotAdminOfMerchant = new(
            "User is not authorized as merchant admin",
            StatusCodes.Status403Forbidden,
            type: "https://wom.social/api/problems/user-not-administrator-of-merchant"
        );

        public readonly static ServiceProblemException SourceNotFound = new(
            "Source not found",
            StatusCodes.Status404NotFound,
            type: "https://wom.social/api/problems/source-not-found"
        );

        public readonly static ServiceProblemException PosNotFound = new(
            "POS not found",
            StatusCodes.Status404NotFound,
            type: "https://wom.social/api/problems/pos-not-found"
        );

        public readonly static ServiceProblemException OwningMerchantOfPosNotFound = new(
            "Owning merchant of POS not found",
            StatusCodes.Status404NotFound,
            type: "https://wom.social/api/problems/owning-merchant-of-pos-not-found"
        );

        public readonly static ServiceProblemException MerchantNotFound = new(
            "Merchant not found",
            StatusCodes.Status404NotFound,
            type: "https://wom.social/api/problems/merchant-not-found"
        );

        public readonly static ServiceProblemException TokenNotValid = new(
            "Token not valid",
            StatusCodes.Status422UnprocessableEntity,
            type: "https://wom.social/api/problems/token-not-valid"
        );

    }
}
