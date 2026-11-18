using System.Text.Json;
using System.Text.Json.Serialization;

namespace tutorial_demo_2.Converters
{
    public class NullableTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var timeString = reader.GetString();
                if (string.IsNullOrEmpty(timeString))
                    return null;

                // Try parsing in different formats
                if (TimeSpan.TryParse(timeString, out TimeSpan result))
                    return result;

                // Try parsing HH:mm format
                if (TimeOnly.TryParse(timeString, out TimeOnly timeOnly))
                    return timeOnly.ToTimeSpan();

                return null;
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing nullable TimeSpan");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString(@"hh\:mm"));
        }
    }
}
