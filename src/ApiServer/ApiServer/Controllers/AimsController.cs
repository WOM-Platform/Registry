using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/aims")]
    public class AimsController : Controller {

        public AimsController(
            IConfiguration configuration,
            DatabaseOperator databaseOperator,
            ILogger<AimsController> logger) {
            Configuration = configuration;
            Database = databaseOperator;
            Logger = logger;
        }

        protected IConfiguration Configuration { get; }
        protected DatabaseOperator Database { get; }
        protected ILogger<AimsController> Logger { get; }

        [Produces("application/json")]
        [HttpGet]
        public IActionResult List() {
            var aims = Database.GetAims();
            return Ok(from a in aims
                      select new {
                          a.Code,
                          a.Description
                      });
        }

    }

}
