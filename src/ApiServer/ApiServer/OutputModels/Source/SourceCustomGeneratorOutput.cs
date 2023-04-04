using System;
using System.Linq;
using System.Text.Json.Serialization;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.OutputModels.Source {
    public class SourceCustomGeneratorOutput {

        public string Title { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PictureOutput Logo { get; init; }

        public bool EnableCustomGeneration { get; init; }

        public class TemplateInfo {

            public string Name { get; init; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Description { get; init; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string Guide { get; init; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int? PresetWomCount { get; init; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string PresetWomAim { get; init; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public GeoCoordsOutput PresetWomLocation { get; init; }

        }

        public TemplateInfo[] Templates { get; init; }

    }

    public static class SourceCustomGeneratorOutputExtensions {
        public static SourceCustomGeneratorOutput ToOutput(this SourceCustomGenerator source, PictureOutput logoPicture) {
            if(source == null) {
                throw new ArgumentNullException();
            }

            return new SourceCustomGeneratorOutput {
                Title = source.Title,
                Logo = logoPicture,
                EnableCustomGeneration = source.EnableCustomGeneration,
                Templates = (from t in source.Templates
                             select new SourceCustomGeneratorOutput.TemplateInfo {
                                 Name = t.Name,
                                 Description = t.Description,
                                 Guide = t.Guide,
                                 PresetWomCount = t.PresetWomCount,
                                 PresetWomAim = t.PresetWomAim,
                                 PresetWomLocation = t.PresetWomLocation.ToOutput(),
                             }).ToArray()
            };
        }
    }
}
