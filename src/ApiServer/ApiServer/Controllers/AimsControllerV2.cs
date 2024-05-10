using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            ILogger<AimsControllerV2> logger)
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
        public ActionResult ListFlat() {
            var aims = _aimService.GetFlatAims();

            return Ok(new AimListResponse {
                Aims = (from a in aims
                        select new AimListResponse.AimNode {
                            Code = a.Code,
                            Titles = a.Titles,
                            Hidden = a.Hidden,
                        }).ToArray(),
            });
        }

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [HttpGet("nested")]
        [HttpHead("nested")]
        [ChangeLog("aim-list")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AimListResponse), StatusCodes.Status200OK)]
        public ActionResult ListNested() {
            var aims = _aimService.GetAims();

            void RecursiveAddToList(IReadOnlyList<AimService.Aim> aims, List<AimListResponse.AimNode> target) {
                foreach(var a in aims) {
                    var node = new AimListResponse.AimNode {
                        Code = a.Code,
                        Titles = a.Titles,
                        Hidden = a.Hidden,
                        Children = [],
                    };

                    if(a.Aims.Length > 0) {
                        var children = new List<AimListResponse.AimNode>();
                        RecursiveAddToList(a.Aims, children);
                        node.Children = children.ToArray();
                    }

                    target.Add(node);
                }
            }
            var output = new List<AimListResponse.AimNode>();
            RecursiveAddToList(aims, output);

            return Ok(new AimListResponse {
                Aims = output.ToArray(),
            });
        }

    }

}
