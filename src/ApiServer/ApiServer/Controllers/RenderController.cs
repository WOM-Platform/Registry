using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("render")]
    [RequireHttpsInProd]
    public class RenderController : Controller {

        private readonly ILogger<RenderController> _logger;

        public RenderController(
            ILogger<RenderController> logger
        ) {
            _logger = logger;
        }

        [HttpGet("qrcode")]
        [Produces("image/png")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult QrCode(
            [FromQuery] string url
        ) {
            var domain = Environment.GetEnvironmentVariable("SELF_HOST");
            if(url == null || !url.StartsWith($"https://{domain}/")) {
                return StatusCode(400);
            }

            var qrCodeData = QRCoder.PngByteQRCodeHelper.GetQRCode(url, QRCoder.QRCodeGenerator.ECCLevel.M, 15);
            return File(qrCodeData, "image/png");
        }

    }

}
