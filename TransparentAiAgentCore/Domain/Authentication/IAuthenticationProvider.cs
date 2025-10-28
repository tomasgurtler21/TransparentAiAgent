namespace TransparentAiAgentCore.Domain.Authentication;

/// <summary>
/// Provides authentication credentials for external services
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Get API key for the specified service
    /// </summary>
    string GetApiKey(string serviceName);

    /// <summary>
    /// Get endpoint URL for the specified service
    /// </summary>
    string GetEndpoint(string serviceName);
}
