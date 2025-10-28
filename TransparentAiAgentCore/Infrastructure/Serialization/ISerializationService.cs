namespace TransparentAiAgentCore.Infrastructure.Serialization;

public interface ISerializationService
{
    /// <summary>
    /// Serialize object to JSON string
    /// </summary>
    string SerializeToJson<T>(T obj);

    /// <summary>
    /// Serialize object to pretty-printed JSON
    /// </summary>
    string SerializeToPrettyJson<T>(T obj);

    /// <summary>
    /// Deserialize JSON string to object
    /// </summary>
    T DeserializeFromJson<T>(string json);
}
