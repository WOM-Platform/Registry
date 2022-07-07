using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {
    [Route("v1/info")]
    public class InfoController : BaseRegistryController {

        private readonly MongoDatabase _mongo;
        private readonly ApiKeyService _apiKeyService;

        public InfoController(
            MongoDatabase mongo,
            ApiKeyService apiKeyService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
            _apiKeyService = apiKeyService;
        }

    }
}
