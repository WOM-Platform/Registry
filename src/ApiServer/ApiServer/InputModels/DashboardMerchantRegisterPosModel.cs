using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels {

    public class DashboardMerchantRegisterPosModel {

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Point of Service Name")]
        public string Name { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Url]
        public string Url { get; set; }

    }

}
