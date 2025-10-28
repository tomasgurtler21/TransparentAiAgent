namespace TransparentAiAgentCore.Domain.Exceptions;

/// <summary>
/// Exception thrown when transparency system encounters an error
/// </summary>
public class TransparencyException : AgentException
{
    public TransparencyException() : base() { }

    public TransparencyException(string message) : base(message) { }

    public TransparencyException(string message, Exception innerException)
        : base(message, innerException) { }
}
