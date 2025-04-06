using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stories.Application.Converters;

public class UnixTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var timestamp = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}