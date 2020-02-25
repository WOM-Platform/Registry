using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

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
        public async Task<IActionResult> Show(int sourceId) {
            var s = await Database.GetSourceById(sourceId);
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
