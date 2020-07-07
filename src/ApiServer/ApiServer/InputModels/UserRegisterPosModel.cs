using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels {

    public class UserRegisterPosModel {

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Nome")]
        public string PosName { get; set; }

        [Url]
        [Display(Name = "URL")]
        public string PosUrl { get; set; }

        [Required]
        [Display(Name = "Latitudine")]
        public double PosLatitude { get; set; }

        [Required]
        [Display(Name = "Longitudine")]
        public double PosLongitude { get; set; }

    }

}
