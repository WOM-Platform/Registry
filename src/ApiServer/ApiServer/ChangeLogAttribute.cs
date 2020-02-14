using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace WomPlatform.Web.Api {

    public class ChangeLogAttribute : ActionFilterAttribute {

        private readonly string _changeLogCode;

        public ChangeLogAttribute(string changeLogCode) {
            _changeLogCode = changeLogCode;
        }

        public override void OnActionExecuting(ActionExecutingContext context) {
            if(string.IsNullOrWhiteSpace(_changeLogCode)) {
                return;
            }

            var db = (DatabaseOperator)context.HttpContext.RequestServices.GetService(typeof(DatabaseOperator));

            var logFactory = (ILoggerFactory)context.HttpContext.RequestServices.GetService(typeof(ILoggerFactory));
            var logger = logFactory.CreateLogger<ChangeLogAttribute>();

            var lastEntry = (from l in db.Data.ChangeLog
                             where l.Code == _changeLogCode
                             orderby l.Timestamp descending
                             select l).FirstOrDefault();
            if(lastEntry == null) {
                logger.LogDebug("No change entry with code '{0}' found", _changeLogCode);
                return;
            }
            logger.LogDebug("Last change entry with code '{0}' at {1}", _changeLogCode, lastEntry.Timestamp);

            context.HttpContext.Response.Headers.TryAdd(HeaderNames.LastModified, new StringValues(lastEntry.Timestamp.ToString("R")));

            if(context.HttpContext.Request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedString)) {
                logger.LogTrace("If-Modified header: {0}", ifModifiedString);
                if(DateTime.TryParseExact(ifModifiedString, "R", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ifModified)) {
                    logger.LogTrace("Checking {0} < {1}", lastEntry.Timestamp, ifModified);
                    if(lastEntry.Timestamp < ifModified) {
                        context.Result = new StatusCodeResult(304);
                    }
                }
            }
            if(context.HttpContext.Request.Headers.TryGetValue(HeaderNames.IfUnmodifiedSince, out var ifUnmodifiedString)) {
                logger.LogTrace("If-Unmodified-Since header: {0}", ifUnmodifiedString);
                if(DateTime.TryParseExact(ifUnmodifiedString, "R", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ifUnmodified)) {
                    logger.LogTrace("Checking {0} >= {1}", lastEntry.Timestamp, ifUnmodified);
                    if(lastEntry.Timestamp >= ifUnmodified) {
                        context.Result = new StatusCodeResult(412);
                    }
                }
            }
        }

    }

}
