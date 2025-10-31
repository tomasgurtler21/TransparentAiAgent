namespace TransparentAiAgentCore.Domain.Configuration;

/// <summary>
/// Specifies the authentication method for Azure OpenAI.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// Authentication mode not specified. This is an invalid state.
    /// Configuration must explicitly specify ApiKey or DefaultAzureCredential.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Use API Key authentication (requires ApiKey in configuration).
    /// Static API key passed in request headers.
    /// </summary>
    ApiKey = 1,

    /// <summary>
    /// Use DefaultAzureCredential authentication (OAuth/Microsoft Entra ID).
    /// Automatically discovers credentials from environment, managed identity, az login, etc.
    /// No API key needed in configuration.
    /// </summary>
    DefaultAzureCredential = 2
}
