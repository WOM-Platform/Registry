using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/migration")]
    [OperationsTags("Voucher migration")]
    public class MigrationController : BaseRegistryController {

        private readonly BackupService _backupService;

        public MigrationController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            BackupService backupService,
            ILogger<MigrationController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _backupService = backupService;
        }

        private static readonly Guid SingleRecord = Guid.NewGuid();

        public record CreateMigrationOutput(
            string RegistryUrl,
            Guid Code,
            string Link,
            DateTime Deadline
        );

        /// <summary>
        /// Register a new migration.
        /// </summary>
        /// <param name="payload">Encrypted file to store as a migration.</param>
        /// <param name="password">Password for the migration, must satisfy security constraints.</param>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CreateMigrationOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Register(
            IFormFile payload,
            [FromForm] string password
        ) {
            if(payload == null || payload.Length == 0) {
                return BadRequest();
            }
            if(!CheckTransferPassword(password, false)) {
                return BadRequest();
            }
            if(payload.Length > 1024 * 1024 * 4) { // More than 4 MB
                return BadRequest();
            }

            using var inputStream = payload.OpenReadStream();
            (var migrationId, var expiresOn) = await _backupService.CreateBackup(inputStream, password);

            return Ok(new CreateMigrationOutput(
                RegistryUrl: $"https://{SelfHostDomain}",
                Code: migrationId,
                Link: $"wom://migration/{migrationId:D}",
                Deadline: expiresOn
            ));
        }

        public record RetrieveMigrationInput(
            string Password
        );

        /// <summary>
        /// Retrieve migration payload.
        /// </summary>
        /// <param name="code">Unique registration code of the required migration.</param>
        [HttpPost("{code}/retrieve")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Retrieve(
            [FromRoute] Guid code,
            [FromBody] RetrieveMigrationInput input
        ) {
            if(code != SingleRecord) {
                return NotFound();
            }
            if(string.IsNullOrWhiteSpace(input?.Password)) {
                return Forbid();
            }

            var rnd = new Random();
            byte[] rndFile = new byte[1024];
            rnd.NextBytes(rndFile);

            return File(rndFile, "application/octet-stream");
        }

        public record GetMigrationInfoOutput(
            int AccessCount,
            DateTime Deadline
        );

        /// <summary>
        /// Retrieves information about a migration.
        /// </summary>
        /// <param name="code">The unique migration code.</param>
        [HttpPost("{code}/info")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GetMigrationInfoOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] Guid code,
            [FromBody] RetrieveMigrationInput input
        ) {
            if(code != SingleRecord) {
                return NotFound();
            }
            if(string.IsNullOrWhiteSpace(input?.Password)) {
                return Forbid();
            }

            return Ok(new GetMigrationInfoOutput(
                1,
                DateTime.UtcNow.AddDays(7)
            ));
        }

    }
}
