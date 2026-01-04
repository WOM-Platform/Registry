using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.Conversion {
    public class JsonPermissiveNumericConverter : JsonConverter<int> {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if(reader.TokenType == JsonTokenType.Number) {
                if(reader.TryGetInt32(out int i)) {
                    return i;
                }
                if(reader.TryGetDouble(out double d)) {
                    return (int)d;
                }

                throw new ArgumentException("Unable to convert JSON value '{0}' to integer", reader.GetString() ?? "unreadable");
            }
            else if(reader.TokenType == JsonTokenType.String) {
                return Convert.ToInt32(reader.GetString());
            }

            throw new ArgumentException("JSON value is not numeric");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value);
        }
    }
}
