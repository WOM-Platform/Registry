using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to a list of aims.
    /// </summary>
    [Route("v2/aims")]
    [OperationsTags("Aims")]
    public class AimsControllerV2 : BaseRegistryController {

        public AimsControllerV2(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<AimsControllerV2> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
        }

        /// <summary>
        /// Single aim output record.
        /// </summary>
        public record AimListEntry(
            string Code,
            Dictionary<string, string> Titles,
            [property:JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            List<AimListEntry> Children
        );

        /// <summary>
        /// Aim list output record.
        /// </summary>
        public record AimListOutput(
            List<AimListEntry> Aims
        );

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [Produces("application/json")]
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        /// <summary>
        /// Get list of all aims.
        /// </summary>
        public async Task<IActionResult> ListV2() {
            var aims = await Mongo.GetAims();

            return Ok(new AimListOutput(
                (from a in aims
                 select new AimListEntry(
                     a.Code,
                     a.Titles,
                     null
                 )).ToList()
            ));
        }

        private void InsertAimEntry(AimListEntry entry, Aim aim, int codeIndex) {
            int parentIndex = -1;
            for(int i = 0; i < entry.Children.Count; ++i) {
                if(aim.Code.StartsWith(entry.Children[i].Code)) {
                    parentIndex = i;
                }
            }

            if(aim.Code.Length == codeIndex + 1) {
                // Must be inserted at this height
                if(parentIndex >= 0) {
                    // At this height, parent and aim to insert must have same code
                    Logger.LogError("Duplicate aim (code {0} already in tree with parent {1})", aim.Code, entry.Children[parentIndex].Code);
                }
                else {
                    entry.Children.Add(new AimListEntry(
                        aim.Code,
                        aim.Titles,
                        new()
                    ));

                    entry.Children.Sort((x, y) => x.Code.CompareTo(y.Code));
                }
            }
            else {
                // Must be inserted down in the tree
                InsertAimEntry(entry.Children[parentIndex], aim, codeIndex + 1);
            }            
        }

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [Produces("application/json")]
        [HttpGet("nested")]
        [HttpHead("nested")]
        [ChangeLog("aim-list")]
        /// <summary>
        /// Get list of all aims.
        /// </summary>
        public async Task<IActionResult> ListNestedV2() {
            var aims = await Mongo.GetAims();

            var fakeRoot = new AimListEntry(null, null, new());
            foreach(var aim in aims) {
                InsertAimEntry(fakeRoot, aim, 0);
            }

            return Ok(new AimListOutput(
                fakeRoot.Children
            ));
        }

    }

}
