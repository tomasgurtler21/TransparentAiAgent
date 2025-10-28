using System.Text.Json;

namespace TransparentAiAgentCore.Infrastructure.Serialization;

public class SerializationService : ISerializationService
{
    private readonly JsonSerializerOptions _standardOptions;
    private readonly JsonSerializerOptions _prettyOptions;

    public SerializationService()
    {
        _standardOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _prettyOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public string SerializeToJson<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return JsonSerializer.Serialize(obj, _standardOptions);
    }

    public string SerializeToPrettyJson<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return JsonSerializer.Serialize(obj, _prettyOptions);
    }

    public T DeserializeFromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or whitespace", nameof(json));

        var result = JsonSerializer.Deserialize<T>(json, _standardOptions);
        if (result == null)
            throw new InvalidOperationException("Deserialization resulted in null");

        return result;
    }
}
