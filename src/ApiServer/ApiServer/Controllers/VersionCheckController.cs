using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels.VersionCheck;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/version-check")]
    [AllowAnonymous]
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

        /// <summary>
        /// Check application version.
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /v1/version-check?platform=Android&amp;currentVersion=0.2.4
        ///
        /// Sample response:
        /// 
        ///     {
        ///         "status: "Ok|ShouldUpdate|MustUpdate",
        ///         "latestVersion": "0.2.4"
        ///     }
        /// </remarks>
        /// <param name="platform">Application platform (Android or iOS).</param>
        /// <param name="currentVersion">Current version of the application, as a semantic version string.</param>
        [HttpPost]
        [ProducesResponseType(typeof(VersionCheckResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        public IActionResult Index(
            [FromQuery] string platform,
            [FromQuery] string currentVersion
        ) {
            platform = platform?.ToLowerInvariant() ?? string.Empty;

            if(!SupportedPlatforms.Any(p => p.Equals(platform)) || !Version.TryParse(currentVersion, out Version inputVersion)) {
                return BadRequest();
            }

            var versionCheckSection = Configuration.GetSection("VersionCheck");
            var platformSection = versionCheckSection.GetSection(platform);

            var latestVersion = Version.Parse(platformSection["LatestVersion"]);
            var minimumVersion = Version.Parse(platformSection["MinimumVersion"]);

            return Ok(new VersionCheckResponse {
                LatestVersion = latestVersion,
                Status = (inputVersion < minimumVersion) ? VersionCheckResponse.UpdateStatus.MustUpdate :
                    (inputVersion < latestVersion) ? VersionCheckResponse.UpdateStatus.ShouldUpdate :
                    VersionCheckResponse.UpdateStatus.Ok
            });
        }

    }

}
