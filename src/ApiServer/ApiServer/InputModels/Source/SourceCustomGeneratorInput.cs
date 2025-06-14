using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Source {
    public class SourceCustomGeneratorInput {
        [Required]
        [StringLength(256)]
        public string Title { get; init; }

        [DefaultValue(true)]
        public bool EnableCustomGeneration { get; init; } = true;

        public class TemplateInfo {
            [Required]
            [StringLength(128, MinimumLength = 4)]
            public string Name { get; init; }

            [StringLength(512)]
            public string? Description { get; init; }

            [StringLength(2048)]
            public string? Guide { get; init; }

            public int? PresetWomCount { get; init; }

            public string? PresetWomAim { get; init; }

            public GeoCoordsInput? PresetWomLocation { get; init; }

            public bool BatchGeneration { get; init; } = false;

        }

        public TemplateInfo[] Templates { get; init; }

    }
}
