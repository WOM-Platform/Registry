using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.OutputModels.Badge {
    public class BadgeChallengeOutput {
        public string Id { get; init; }

        public bool IsPublic { get; init; }

        public Dictionary<string, string> Name { get; init; }

        public Dictionary<string, string> Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InformationUrl { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BadgeOutput[] Badges { get; init; }
    }

    public static class BadgeChallengeOutputExtensions {
        public static BadgeChallengeOutput ToOutput(this DatabaseDocumentModels.BadgeChallenge challenge, IEnumerable<DatabaseDocumentModels.Badge> badges, PicturesService pictureService) {
            return new BadgeChallengeOutput {
                Id = challenge.Id.ToString(),
                IsPublic = challenge.IsPublic,
                Name = challenge.Name,
                Description = challenge.Description,
                InformationUrl = challenge.InformationUrl,
                Badges = badges.Any() ?
                    (from badge in badges select badge.ToOutput(pictureService)).ToArray() :
                    null,
            };
        }
    }
}
