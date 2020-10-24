using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels {

    public class UserRegisterPosModelOptional {

        [StringLength(128)]
        [Display(Name = "Name")]
        public string PosName { get; set; }

        [Url]
        [Display(Name = "URL")]
        public string PosUrl { get; set; }

        [Display(Name = "Latitude")]
        public double? PosLatitude { get; set; }

        [Display(Name = "Longitude")]
        public double? PosLongitude { get; set; }

        public bool IsSet() {
            return (
                !string.IsNullOrWhiteSpace(PosName) &&
                PosLatitude.HasValue &&
                PosLongitude.HasValue
            );
        }

    }

}
