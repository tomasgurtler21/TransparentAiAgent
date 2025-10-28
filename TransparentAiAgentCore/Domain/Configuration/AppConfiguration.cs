namespace TransparentAiAgentCore.Domain.Configuration;

public class AppConfiguration
{
    public AgentConfiguration Agent { get; set; } = new();
    public LLMConfiguration LLM { get; set; } = new();
    public MCPConfiguration MCP { get; set; } = new();

    public void Validate()
    {
        Agent.Validate();
        LLM.Validate();
        MCP.Validate();
    }
}
