using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Badge {
    public class RegisterBadgeChallengeInput {
        [Required]
        public bool IsPublic { get; init; }

        [Required]
        public Dictionary<string, string> Name { get; init; }

        public Dictionary<string, string>? Description { get; init; }

        [Url]
        [MaxLength(2048)]
        public string? InformationUrl { get; init; }
    }
}
