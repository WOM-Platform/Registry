using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.OutputModels.Badge {
    public class BadgeOutput {
        public string Id { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ChallengeId { get; init; }

        public bool IsPublic { get; init; }

        public Dictionary<string, string> Name { get; init; }

        public BadgeSimpleFilterOutput? SimpleFilter { get; init; }

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
        public static BadgeOutput ToOutput(this DatabaseDocumentModels.Badge badge, PicturesService pictureService) {
            return new BadgeOutput {
                Id = badge.Id.ToString(),
                ChallengeId = badge.ChallengeId?.ToString(),
                IsPublic = badge.IsPublic,
                Name = badge.Name.ToOutput(),
                SimpleFilter = badge.SimpleFilter.ToOutput(),
                Image = pictureService.GetPictureOutput(badge.ImagePath, badge.ImageBlurHash),
                Description = badge.Description.ToOutput(),
                InformationUrl = badge.InformationUrl,
                CreatedAt = badge.CreatedAt,
                LastUpdate = badge.LastUpdate,
            };
        }
    }
}
