using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.OutputModels.VersionCheck;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/version-check")]
    [OperationsTags("Application configuration")]
    [AllowAnonymous]
    [RequireHttpsInProd]
    public class VersionCheckController : BaseRegistryController {

        public VersionCheckController(
            IServiceProvider serviceProvider,
            ILogger<VersionCheckController> logger)
        : base(serviceProvider, logger) {

        }

        private readonly string[] SupportedApplications = [
            "pocket",
            "pos"
        ];

        private readonly string[] SupportedPlatforms = [
            "ios",
            "android"
        ];

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
            [FromQuery] string currentVersion,
            [FromQuery] string application = "pocket"
        ) {
            application = application?.ToLowerInvariant() ?? string.Empty;
            if(!SupportedApplications.Any(p => p.Equals(application))) {
                return BadRequest();
            }

            platform = platform?.ToLowerInvariant() ?? string.Empty;
            if(!SupportedPlatforms.Any(p => p.Equals(platform)) || !Version.TryParse(currentVersion, out Version inputVersion)) {
                return BadRequest();
            }

            var versionCheckSection = Configuration.GetSection("VersionCheck");
            var applicationSection = versionCheckSection.GetSection(application);
            var platformSection = applicationSection.GetSection(platform);

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
