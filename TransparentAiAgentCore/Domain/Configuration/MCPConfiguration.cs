namespace TransparentAiAgentCore.Domain.Configuration;

public class MCPConfiguration
{
    public List<MCPServerConfiguration> Servers { get; set; } = new();

    public void Validate()
    {
        foreach (var server in Servers)
        {
            server.Validate();
        }
    }
}
