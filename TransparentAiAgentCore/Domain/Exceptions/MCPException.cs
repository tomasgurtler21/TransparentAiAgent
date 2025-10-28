namespace TransparentAiAgentCore.Domain.Exceptions;

/// <summary>
/// Exception thrown when MCP client or tool execution encounters an error
/// </summary>
public class MCPException : AgentException
{
    public MCPException() : base() { }

    public MCPException(string message) : base(message) { }

    public MCPException(string message, Exception innerException)
        : base(message, innerException) { }
}
