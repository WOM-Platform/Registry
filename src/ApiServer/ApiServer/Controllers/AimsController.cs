using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/aims")]
    public class AimsController : BaseRegistryController {

        public AimsController(
            IConfiguration configuration,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, database, logger) {
        }

        [Produces("application/json")]
        [HttpGet]
        public IActionResult List() {
            var aims = Database.GetAims();
            return Ok(from a in aims
                      select new {
                          a.Code,
                          a.IconFile,
                          Titles = (from t in a.Titles
                                    select new {
                                        t.LanguageCode,
                                        t.Title
                                    }).ToList()
                      });
        }

    }

}
