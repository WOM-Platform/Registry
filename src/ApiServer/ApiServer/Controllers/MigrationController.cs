using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/migration")]
    [OperationsTags("Voucher migration")]
    public class MigrationController : BaseRegistryController {

        public MigrationController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<MigrationController> logger)
        : base(configuration, crypto, keyManager, logger) {

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
        public IActionResult Register(
            IFormFile payload,
            [FromForm] string password
        ) {
            if(payload == null || payload.Length == 0) {
                return BadRequest();
            }
            if(!CheckTransferPassword(password)) {
                return BadRequest();
            }

            return Ok(new CreateMigrationOutput(
                RegistryUrl: $"https://{SelfHostDomain}",
                Code: SingleRecord,
                Link: $"wom://migration/{SingleRecord:D}",
                Deadline: DateTime.UtcNow.AddDays(7)
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
        public IActionResult GetInformation(
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
