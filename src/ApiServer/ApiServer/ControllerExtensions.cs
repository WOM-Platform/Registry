using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api {

    public static class ControllerExtensions {

        public static ActionResult ProblemParameter(this ControllerBase c, string title = null) {
            return new ProblemResult(
                422, // Unprocessable Entity, see https://httpstatuses.com/422
                "wrong-parameter",
                title ?? "Request parameter not valid"
            );
        }

        public static ActionResult RequestVoid(this ControllerBase c, string title = null) {
            return new ProblemResult(
                410, // Gone, see https://httpstatuses.com/410
                "request-void",
                title ?? "Request instance is void"
            );
        }

        public static ActionResult UnexpectedError(this ControllerBase c, string title = null) {
            return new ProblemResult(
                500,
                "unexpected",
                title ?? "Unexpected internal error"
            );
        }

    }

}
