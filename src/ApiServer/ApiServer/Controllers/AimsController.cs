using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/aims")]
    public class AimsController : BaseRegistryController {

        public AimsController(
            IConfiguration configuration,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, database, logger) {
        }

        private object AimToNodeObject(IndexedNodeOf<char, Aim> aim) {
            return new {
                aim.Item.Code,
                aim.Item.IconFile,
                Titles = aim.Item.Titles.ToDictionary(t => t.LanguageCode, t => t.Title),
                Children = from sub in aim.Children.Values select AimToNodeObject(sub)
            };
        }

        private IList<object> AimsToFlatList(IDictionary<char, IndexedNodeOf<char, Aim>> aims) {
            List<object> ret = new List<object>();

            void AddToList(IList<object> list, IndexedNodeOf<char, Aim> item) {
                list.Add(new {
                    item.Item.Code,
                    item.Item.IconFile,
                    Titles = item.Item.Titles.ToDictionary(t => t.LanguageCode, t => t.Title),
                });

                foreach(var subitem in item.Children) {
                    AddToList(list, subitem.Value);
                }
            }

            foreach(var item in aims) {
                AddToList(ret, item.Value);
            }

            return ret;
        }

        [Produces("application/json")]
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        public IActionResult List(string format = "hierarchical") {
            var aims = Database.GetAimHierarchy();

            var obj = format.Equals("flat", StringComparison.InvariantCultureIgnoreCase) ?
                AimsToFlatList(aims) :
                from a in aims.Values select AimToNodeObject(a);

            return Ok(obj);
        }

    }

}
