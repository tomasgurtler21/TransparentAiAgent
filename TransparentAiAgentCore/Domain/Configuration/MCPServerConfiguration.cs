using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class MCPServerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string?> Env { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ConfigurationException("MCP Server Name cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Command))
            throw new ConfigurationException("MCP Server Command cannot be null or whitespace");
    }
}
