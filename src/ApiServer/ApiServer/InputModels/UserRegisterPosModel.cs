using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels {

    public class UserRegisterPosModel {

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Name")]
        public string PosName { get; set; }

        [Url]
        [Display(Name = "URL")]
        public string PosUrl { get; set; }

        [Required]
        [Display(Name = "Latitude")]
        public double PosLatitude { get; set; }

        [Required]
        [Display(Name = "Longitude")]
        public double PosLongitude { get; set; }

    }

}
