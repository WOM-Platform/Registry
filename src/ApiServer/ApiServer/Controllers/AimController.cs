using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/aim")]
    public class AimController : BaseRegistryController {

        public AimController(
            IConfiguration configuration,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<AimController> logger)
        : base(configuration, crypto, keyManager, database, logger) {
        }

        [HttpGet("{*code}")]
        public IActionResult Show(string code) {
            var aim = Database.GetAimByCode(code);
            if(aim == null) {
                return NotFound();
            }

            var subaims = Database.GetSubAims(aim);

            if(Request.HasAcceptHeader("text/html")) {
                return Content("Aim in HTML");
            }
            else {
                return Ok(new {
                    aim.Code,
                    aim.Description,
                    SubAims = (from sa in subaims
                               select new {
                                   sa.Code,
                                   sa.Description
                               }).ToList()
                });
            }
        }

    }

}
