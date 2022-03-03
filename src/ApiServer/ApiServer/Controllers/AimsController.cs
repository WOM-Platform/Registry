﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to a list of aims.
    /// </summary>
    [Route("v1/aims")]
    [OperationsTags("Aims")]
    public class AimsController : BaseRegistryController {

        private readonly MongoDatabase _mongo;

        public AimsController(
            MongoDatabase mongo,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
        }

        public record AimListEntryOutput(
            string Code,
            Dictionary<string, string> Titles,
            int Order
        );

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AimListEntryOutput[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> List() {
            var aims = await _mongo.GetAims();
            return Ok(from a in aims
                      select new AimListEntryOutput(
                          a.Code,
                          a.Titles,
                          a.Order
                      ));
        }

    }

}
