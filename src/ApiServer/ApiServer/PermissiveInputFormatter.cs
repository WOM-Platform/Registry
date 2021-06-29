using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Allows reading of input without content-type header.
    /// </summary>
    public class PermissiveInputFormatter : IInputFormatter {

        public bool CanRead(InputFormatterContext context) {
            var contentType = context.HttpContext.Request.ContentType;
            if (string.IsNullOrEmpty(contentType)) {
                return true;
            }
            return false;
        }

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context) {
            return InputFormatterResult.SuccessAsync(null);
        }

    }

}
