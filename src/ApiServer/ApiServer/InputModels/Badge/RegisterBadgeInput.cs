using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WomPlatform.Web.Api.InputModels.Badge {
    public class RegisterBadgeInput {
        public string? ChallengeId { get; init; }

        [Required]
        public bool IsPublic { get; init; }

        [Required]
        public Dictionary<string, string> Name { get; init; }

        public Dictionary<string, string> Description { get; init; }

        [Url]
        [MaxLength(2048)]
        public string? InformationUrl { get; init; }
    }
}
