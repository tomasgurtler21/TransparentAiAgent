namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// Specifies the authentication method for Azure OpenAI.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// Use API Key authentication (requires ApiKey in configuration).
    /// Static API key passed in request headers.
    /// </summary>
    ApiKey,

    /// <summary>
    /// Use DefaultAzureCredential authentication (OAuth/Microsoft Entra ID).
    /// Automatically discovers credentials from environment, managed identity, az login, etc.
    /// No API key needed in configuration.
    /// </summary>
    DefaultAzureCredential
}
