using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class MCPServerConfigurationTests
{
    [TestMethod]
    public void Validate_NullName_ThrowsConfigurationException()
    {
        // Arrange
        var config = new MCPServerConfiguration { Name = null! };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyName_ThrowsConfigurationException()
    {
        // Arrange
        var config = new MCPServerConfiguration { Name = "" };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_WhitespaceName_ThrowsConfigurationException()
    {
        // Arrange
        var config = new MCPServerConfiguration { Name = "   " };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_NullCommand_ThrowsConfigurationException()
    {
        // Arrange
        var config = new MCPServerConfiguration
        {
            Name = "test-server",
            Command = null!
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyCommand_ThrowsConfigurationException()
    {
        // Arrange
        var config = new MCPServerConfiguration
        {
            Name = "test-server",
            Command = ""
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_WhitespaceCommand_ThrowsConfigurationException()
    {
        // Arrange
        var config = new MCPServerConfiguration
        {
            Name = "test-server",
            Command = "   "
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new MCPServerConfiguration
        {
            Name = "test-server",
            Command = "npx",
            Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" },
            Env = new Dictionary<string, string> { { "PATH", "/usr/bin" } }
        };

        // Act & Assert - Should not throw
        config.Validate();
    }
}
