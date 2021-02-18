using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api {

    public class ProblemResult : ActionResult {

        public ProblemResult(int statusCode, string type, string title) {
            StatusCode = statusCode;
            ProblemType = type;
            ProblemTitle = title;
        }

        public int StatusCode { get; }
        public string ProblemType { get; }
        public string ProblemTitle { get; }

        /// <inheritdoc />
        public override void ExecuteResult(ActionContext context) {
            var resp = context.HttpContext.Response;
            resp.StatusCode = StatusCode;
            resp.ContentType = "application/problem+json";
            resp.Headers["Content-Language"] = "en";

            using(var tw = new StreamWriter(resp.Body)) {
                tw.Write(JsonSerializer.Serialize(new {
                    type = ProblemType,
                    title = ProblemTitle
                }));
            }
        }

    }

}
