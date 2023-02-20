using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels.Aim;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to a list of aims.
    /// </summary>
    [Route("v2/aims")]
    [OperationsTags("Aims")]
    [RequireHttpsInProd]
    public class AimsControllerV2 : BaseRegistryController {

        private readonly AimService _aimService;

        public AimsControllerV2(
            AimService aimService,
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
            _aimService = aimService;
        }

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AimListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListV2() {
            var aims = await _aimService.GetAllAims();

            return Ok(new AimListResponse {
                Aims = (from a in aims
                        select new AimListResponse.AimNode {
                            Code = a.Code,
                            Titles = a.Titles,
                            Hidden = a.Hidden,
                            Children = null,
                        }).ToArray()
            });
        }

        private void InsertAimEntry(AimListResponse.AimNode entry, Aim aim, int level) {
            // Insertion point is at first character "larger" than current
            // Starts at -1, which indicates insertion at the back
            int insertionIndex = -1;
            for(int i = 0; i < entry.Children.Length; ++i) {
                if(entry.Children[i].Code[level] == aim.Code[level]) {
                    // First character matches with first character of insertion point, we must insert at a lower level
                    InsertAimEntry(entry.Children[i], aim, level + 1);
                    return;
                }
                else if(entry.Children[i].Code[level] > aim.Code[level]) {
                    insertionIndex = i;
                    break;
                }
            }
            if(insertionIndex == -1) {
                insertionIndex = entry.Children.Length; // Append at end
            }

            var newChildren = new AimListResponse.AimNode[entry.Children.Length + 1];
            Array.Copy(entry.Children[..insertionIndex], newChildren, insertionIndex);
            newChildren[insertionIndex] = new AimListResponse.AimNode {
                Code = aim.Code,
                Titles = aim.Titles,
                Hidden = aim.Hidden,
                Children = Array.Empty<AimListResponse.AimNode>(),
            };
            Array.Copy(entry.Children[insertionIndex..], 0, newChildren, insertionIndex + 1, entry.Children.Length - insertionIndex);

            entry.Children = newChildren;
        }

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [HttpGet("nested")]
        [HttpHead("nested")]
        [ChangeLog("aim-list")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AimListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListNestedV2() {
            var aims = await _aimService.GetAllAims();

            var root = new AimListResponse.AimNode { Children = Array.Empty<AimListResponse.AimNode>() };
            foreach(var aim in aims) {
                InsertAimEntry(root, aim, 0);
            }

            return Ok(new AimListResponse {
                Aims = root.Children
            });
        }

    }

}
