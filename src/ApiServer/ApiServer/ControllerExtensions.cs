using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api {

    public static class ControllerExtensions {

        public static ActionResult ProblemParameter(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status422UnprocessableEntity, // See https://httpstatuses.com/422
                    Type = "https://wom.social/api/problems/wrong-parameter",
                    Title = title ?? "Request parameter not valid"
                }
            );
        }

        public static ActionResult RequestVoid(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status410Gone, // See https://httpstatuses.com/410
                    Type = "https://wom.social/api/problems/request-void",
                    Title = title ?? "Request instance is void"
                }
            );
        }

        public static ActionResult UnexpectedError(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://wom.social/api/problems/unexpected",
                    Title = title ?? "Unexpected internal error"
                }
            );
        }

        public static ActionResult SourceNotFound(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://wom.social/api/problems/source-not-found",
                    Title = "Source with the specified ID does not exist"
                }
            );
        }

        public static ActionResult PosNotFound(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://wom.social/api/problems/pos-not-found",
                    Title = "POS with the specified ID does not exist"
                }
            );
        }

        public static ActionResult SourceVerificationFailure(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status401Unauthorized,
                    Type = "https://wom.social/api/problems/source-verification-failed",
                    Title = "Failed to verify source identity"
                }
            );
        }

        public static ActionResult PayloadVerificationFailure(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status403Forbidden,
                    Type = "https://wom.social/api/problems/payload-verification-failure",
                    Title = title ?? "Failed to verify request contents"
                }
            );
        }

        public static ActionResult PasswordUnacceptableFailure(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status422UnprocessableEntity, // See https://httpstatuses.com/422
                    Type = "https://wom.social/api/problems/password-unacceptable",
                    Title = title ?? "Supplied password is not acceptable"
                }
            );
        }

    }

}
