using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api {

    public static class ControllerExtensions {

        public static ActionResult ProblemParameter(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = 422, // Unprocessable Entity, see https://httpstatuses.com/422
                    Type = "wrong-parameter",
                    Title = title ?? "Request parameter not valid"
                }
            );
        }

        public static ActionResult RequestVoid(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = 410, // Gone, see https://httpstatuses.com/410
                    Type = "request-void",
                    Title = title ?? "Request instance is void"
                }
            );
        }

        public static ActionResult UnexpectedError(this ControllerBase c, string title = null) {

            return new ObjectResult(
                new ProblemDetails {
                    Status = 500,
                    Type = "unexpected",
                    Title = title ?? "Unexpected internal error"
                }
            );
        }

        public static ActionResult SourceNotFound(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = 404,
                    Type = "source-not-found",
                    Title = "Source with the specified ID does not exist"
                }
            );
        }

        public static ActionResult PosNotFound(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = 404,
                    Type = "pos-not-found",
                    Title = "POS with the specified ID does not exist"
                }
            );
        }

        public static ActionResult SourceVerificationFailure(this ControllerBase c) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = 401,
                    Type = "source-verification-failed",
                    Title = "Failed to verify source identity"
                }
            );
        }

        public static ActionResult PayloadVerificationFailure(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = 403,
                    Type = "payload-verification-failure",
                    Title = title ?? "Failed to verify request contents"
                }
            );
        }

        public static ActionResult PasswordUnacceptableFailure(this ControllerBase c, string title = null) {
            return new ObjectResult(
                new ProblemDetails {
                    Status = 422, // Unprocessable Entity, see https://httpstatuses.com/422
                    Type = "password-unacceptable",
                    Title = title ?? "Supplied password is not acceptable"
                }
            );
        }

    }

}
