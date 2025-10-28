using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class MCPConfigurationTests
{
    [TestMethod]
    public void Validate_EmptyServersList_DoesNotThrow()
    {
        // Arrange
        var config = new MCPConfiguration
        {
            Servers = new List<MCPServerConfiguration>()
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    [TestMethod]
    public void Validate_ValidServersList_DoesNotThrow()
    {
        // Arrange
        var config = new MCPConfiguration
        {
            Servers = new List<MCPServerConfiguration>
            {
                new MCPServerConfiguration
                {
                    Name = "server1",
                    Command = "npx"
                },
                new MCPServerConfiguration
                {
                    Name = "server2",
                    Command = "node"
                }
            }
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    [TestMethod]
    public void Validate_InvalidServer_ThrowsConfigurationException()
    {
        // Arrange
        var config = new MCPConfiguration
        {
            Servers = new List<MCPServerConfiguration>
            {
                new MCPServerConfiguration
                {
                    Name = "valid-server",
                    Command = "npx"
                },
                new MCPServerConfiguration
                {
                    Name = "", // Invalid
                    Command = "node"
                }
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }
}
