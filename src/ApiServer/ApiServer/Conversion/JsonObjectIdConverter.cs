using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.Conversion {

    class JsonObjectIdConverter : JsonConverter<ObjectId> {

        public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            if(value == null) {
                throw new ArgumentException("ID must be set as string");
            }

            if(!ObjectId.TryParse(value, out ObjectId id)) {
                throw new ArgumentException("ID must conform to MongoDB format");
            }

            return id;
        }

        public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }

    }

}
