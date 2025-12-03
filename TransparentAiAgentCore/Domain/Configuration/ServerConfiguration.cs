using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore.Domain.Configuration;

public class ServerConfiguration
{
    /// <summary>
    /// HTTP port for the web server. Default is 5025.
    /// </summary>
    public int HttpPort { get; set; } = 5025;

    /// <summary>
    /// HTTPS port for the web server. Default is 7299.
    /// </summary>
    public int HttpsPort { get; set; } = 7299;

    public void Validate()
    {
        if (HttpPort < 1 || HttpPort > 65535)
            throw new ConfigurationException($"HttpPort must be between 1 and 65535, but was {HttpPort}");

        if (HttpsPort < 1 || HttpsPort > 65535)
            throw new ConfigurationException($"HttpsPort must be between 1 and 65535, but was {HttpsPort}");

        if (HttpPort == HttpsPort)
            throw new ConfigurationException($"HttpPort and HttpsPort cannot be the same ({HttpPort})");
    }
}
