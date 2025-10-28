namespace TransparentAiAgentCore.Domain.Exceptions;

/// <summary>
/// Exception thrown when configuration is invalid or missing
/// </summary>
public class ConfigurationException : AgentException
{
    public ConfigurationException() : base() { }

    public ConfigurationException(string message) : base(message) { }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
