using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels.Badge {
    public class BadgeOutput {
        public string Id { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ChallengeId { get; init; }

        public bool IsPublic { get; init; }

        public Dictionary<string, string> Name { get; init; }

        // public BadgeSimpleFilter? SimpleFilter { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PictureOutput? Image { get; init; }

        public Dictionary<string, string> Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InformationUrl { get; init; }

        public DateTime CreatedAt { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? LastUpdate { get; init; }
    }

    public static class BadgeOutputExtensions {
        public static BadgeOutput ToOutput(this DatabaseDocumentModels.Badge badge, PictureOutput? imageOutput) {
            return new BadgeOutput {
                Id = badge.Id.ToString(),
                ChallengeId = badge.ChallengeId != null ? badge.ChallengeId.ToString() : null,
                IsPublic = badge.IsPublic,
                Name = badge.Name,
                Image = imageOutput,
                Description = badge.Description,
                InformationUrl = badge.InformationUrl,
                CreatedAt = badge.CreatedAt,
                LastUpdate = badge.LastUpdate,
            };
        }
    }
}
