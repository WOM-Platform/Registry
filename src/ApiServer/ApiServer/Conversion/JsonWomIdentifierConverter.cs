using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api.Conversion {

    public class JsonWomIdentifierConverter : JsonConverter<Identifier> {

        public override Identifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if(reader.TokenType == JsonTokenType.String) {
                return new Identifier(reader.GetString());
            }
            else if(reader.TokenType == JsonTokenType.Number) {
                return new Identifier(reader.GetInt64());
            }

            throw new ArgumentException("WOM identifier not string nor number");
        }

        public override void Write(Utf8JsonWriter writer, Identifier value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.Id);
        }

    }

}
