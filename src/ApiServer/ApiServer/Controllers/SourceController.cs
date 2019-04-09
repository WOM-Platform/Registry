using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/source")]
    public class SourceController : BaseRegistryController {

        public SourceController(
            IConfiguration configuration,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<SourceController> logger)
        : base(configuration, crypto, keyManager, database, logger) {
        }

        [HttpGet("{sourceId}")]
        public IActionResult Show(int sourceId) {
            var s = Database.GetSourceById(sourceId);
            if(s == null) {
                return NotFound();
            }

            if(Request.HasAcceptHeader("text/html")) {
                return View("Show", s);
            }
            else {
                return Ok(new {
                    s.Id,
                    s.Name,
                    s.Url,
                    Contact = new {
                        s.Contact.Name,
                        s.Contact.Surname,
                        s.Contact.Email
                    }
                });
            }
        }

    }

}
