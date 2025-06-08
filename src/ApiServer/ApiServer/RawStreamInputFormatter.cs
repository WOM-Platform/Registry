using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace WomPlatform.Web.Api {
    public class RawStreamInputFormatter : InputFormatter {
        public RawStreamInputFormatter() {
            SupportedMediaTypes.Add("application/octet-stream");
            SupportedMediaTypes.Add("plain/text");
            SupportedMediaTypes.Add("image/*");
        }

        public override bool CanRead(InputFormatterContext context) {
            ArgumentNullException.ThrowIfNull(context);

            // We can read everything
            return true;
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context) {
            var request = context.HttpContext.Request;
            var contentType = context.HttpContext.Request.ContentType;

            if("text/plain".Equals(contentType)) {
                using var reader = new StreamReader(request.Body);
                var textContent = await reader.ReadToEndAsync();
                return await InputFormatterResult.SuccessAsync(textContent);
            }
            else {
                MemoryStream ms = new(2048);
                context.HttpContext.Response.OnCompleted(() => {
                    ms.Dispose();
                    return Task.CompletedTask;
                });

                await request.Body.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);

                return await InputFormatterResult.SuccessAsync(ms);
            }
        }
    }
}
