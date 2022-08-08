using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Driver.GeoJsonObjectModel;

namespace GuidEncodingConversion {
    internal class GeoJsonPointConverter : JsonConverter<GeoJsonPoint<GeoJson2DGeographicCoordinates>> {
        public override GeoJsonPoint<GeoJson2DGeographicCoordinates>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if(reader.TokenType != JsonTokenType.StartArray) {
                throw new JsonException("Expected start of array");
            }
            reader.Read();
            var lat = reader.GetDouble();
            reader.Read();
            var lng = reader.GetDouble();
            reader.Read();
            if(reader.TokenType != JsonTokenType.EndArray) {
                throw new JsonException("Expected end of array");
            }

            return new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(lng, lat));
        }

        public override void Write(Utf8JsonWriter writer, GeoJsonPoint<GeoJson2DGeographicCoordinates> value, JsonSerializerOptions options) {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Coordinates.Latitude);
            writer.WriteNumberValue(value.Coordinates.Longitude);
            writer.WriteEndArray();
        }
    }
}
