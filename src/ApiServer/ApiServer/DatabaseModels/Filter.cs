using System;

namespace WomPlatform.Web.Api.DatabaseModels {

    /// <summary>
    /// Filter model that encloses supported filter types (only simple ATM).
    /// </summary>
    /// <remarks>
    /// Notice that, for simplicity, we are re-using controller models here.
    /// </remarks>
    public class Filter {

        public Models.SimpleFilter Simple { get; set; }

        // TODO: add complex filter models

    }

}
