using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/migration")]
    [OperationsTags("Voucher migration")]
    [RequireHttpsInProd]
    public class MigrationController : BaseRegistryController {

        private readonly BackupService _backupService;

        public MigrationController(
            BackupService backupService,
            IServiceProvider serviceProvider,
            ILogger<MigrationController> logger)
        : base(serviceProvider, logger) {
            _backupService = backupService;
        }

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
                RegistryUrl: SelfHostDomain,
                Code: migrationId,
                Link: $"https://{SelfLinkDomain}/migration/{migrationId:D}",
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
        public async Task<ActionResult> Retrieve(
            [FromRoute] Guid code,
            [FromBody] RetrieveMigrationInput input
        ) {
            (var exists, var accessGranted, var file) = await _backupService.RetrieveBackup(code, input?.Password);
            if(!exists) {
                return NotFound();
            }
            if(!accessGranted) {
                return Forbid();
            }

            return File(file, "application/octet-stream");
        }

        public record MarkMigrationCompleteInput(
            string Password
        );

        /// <summary>
        /// Mark migration as completed.
        /// </summary>
        /// <param name="code">Unique registration code of the required migration.</param>
        [HttpPost("{code}/complete")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> MarkAsCompleted(
            [FromRoute] Guid code,
            [FromBody] MarkMigrationCompleteInput input
        ) {
            bool found = await _backupService.MarkBackupAsCompleted(code, input?.Password);
            if(!found) {
                return NotFound();
            }

            return Ok();
        }

        public record GetMigrationInfoOutput(
            int AccessCount,
            DateTime Deadline,
            bool Completed
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
            (var exists, var accessGranted, var migration) = await _backupService.GetBackupInformation(code, input.Password);
            if(!exists) {
                return NotFound();
            }
            if(!accessGranted) {
                return Forbid();
            }

            return Ok(new GetMigrationInfoOutput(
                migration.AccessCount,
                migration.ExpiresOn,
                migration?.IsCompleted ?? false
            ));
        }

    }
}
