using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Pos {
    public class PosUpdateInput {
        [Required]
        [StringLength(128, MinimumLength = 4)]
        public string Name { get; set; }

        [StringLength(4096)]
        public string Description { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Url]
        public string Url { get; set; }

        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;
    }
}
