using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels.VersionCheck;

namespace WomPlatform.Web.Api.Controllers {

    [ApiVersion("1")]
    [Route("v{version:apiVersion}/version-check")]
    [OperationsTags("Application configuration")]
    public class VersionCheckController : BaseRegistryController {

        public VersionCheckController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<VersionCheckController> logger)
        : base(configuration, crypto, keyManager, logger) {
            
        }

        private readonly string[] SupportedPlatforms = new string[] {
            "ios",
            "android"
        };

        [HttpPost]
        public IActionResult Index(
            [FromQuery] string platform,
            [FromQuery] Version currentVersion
        ) {
            platform = platform?.ToLowerInvariant() ?? string.Empty;

            if(!SupportedPlatforms.Any(p => p.Equals(platform)) || currentVersion == null) {
                return BadRequest();
            }

            var versionCheckSection = Configuration.GetSection("VersionCheck");
            var platformSection = versionCheckSection.GetSection(platform);

            var latestVersion = Version.Parse(platformSection["LatestVersion"]);
            var minimumVersion = Version.Parse(platformSection["MinimumVersion"]);

            return Ok(new VersionCheckResponse {
                LatestVersion = latestVersion,
                Status = (currentVersion < minimumVersion) ? VersionCheckResponse.UpdateStatus.MustUpdate :
                    (currentVersion < latestVersion) ? VersionCheckResponse.UpdateStatus.ShouldUpdate :
                    VersionCheckResponse.UpdateStatus.Ok
            });
        }

    }

}
