using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.User {
    public class UserCreationOutput {
        public ObjectId Id { get; set; }

        public string Email { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string GeneratedPassword { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }
    }
}
