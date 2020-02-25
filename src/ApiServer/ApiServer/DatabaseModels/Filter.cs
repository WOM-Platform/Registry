using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api.DatabaseModels {

    /// <summary>
    /// Filter model that encloses supported filter types (only simple ATM).
    /// </summary>
    /// <remarks>
    /// Notice that, for simplicity, we are re-using controller models here.
    /// </remarks>
    public class Filter {

        public SimpleFilter Simple { get; set; }

        // TODO: add complex filter models

    }

}
