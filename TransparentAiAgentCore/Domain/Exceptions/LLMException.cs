namespace TransparentAiAgentCore.Domain.Exceptions;

/// <summary>
/// Exception thrown when LLM provider encounters an error
/// </summary>
public class LLMException : AgentException
{
    public LLMException() : base() { }

    public LLMException(string message) : base(message) { }

    public LLMException(string message, Exception innerException)
        : base(message, innerException) { }
}
