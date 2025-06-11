using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Skaar.ServiceBusEmulatorClient.Serializing;

public class ParsableJsonConverter<T> : JsonConverter<T> where T : IParsable<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return T.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString());
    }
}