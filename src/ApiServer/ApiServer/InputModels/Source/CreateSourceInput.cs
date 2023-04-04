using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Source {
    public class CreateSourceInput {
        [Required]
        [StringLength(64, MinimumLength = 4)]
        public string Name { get; set; }

        [Url]
        public string Url { get; set; }
    }
}
