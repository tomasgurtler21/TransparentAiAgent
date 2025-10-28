namespace TransparentAiAgentCore.Domain.Exceptions;

/// <summary>
/// Base exception for all agent-related errors
/// </summary>
public class AgentException : Exception
{
    public AgentException() : base() { }

    public AgentException(string message) : base(message) { }

    public AgentException(string message, Exception innerException)
        : base(message, innerException) { }
}
