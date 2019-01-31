using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
                tw.Write(JsonConvert.SerializeObject(new {
                    type = ProblemType,
                    title = ProblemTitle
                }, Formatting.None));
            }
        }

    }

}
