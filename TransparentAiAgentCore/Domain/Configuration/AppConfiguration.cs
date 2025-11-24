using System.Text.Json.Serialization;

namespace TransparentAiAgentCore.Domain.Configuration;

public class AppConfiguration
{
    public AgentConfiguration Agent { get; set; } = new();
    public LLMConfiguration LLM { get; set; } = new();

    // Tools configuration is loaded from a separate tools.json file
    [JsonIgnore]
    public ToolsConfiguration Tools { get; set; } = new();

    public void Validate()
    {
        Agent.Validate();
        LLM.Validate();
        Tools.Validate();
    }
}
