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

        public static ActionResult UserNotFound(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://wom.social/api/problems/user-not-found",
                    Title = "User with the specified ID does not exist"
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

        public static ActionResult OtcNotFound(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://wom.social/api/problems/otc-not-valid",
                    Title = "OTC code does not exist"
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

        public static ActionResult DeleteFailed(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://wom.social/api/problems/delete-failed",
                    Title = title ?? "Delete operation has failed"
                }
            );
        }

        public static ActionResult WriteFailed(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://wom.social/api/problems/write-failed",
                    Title = title ?? "Write operation has failed"
                }
            );
        }

    }

}
