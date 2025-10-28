using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class AnthropicConfigurationTests
{
    [TestMethod]
    public void Validate_NullApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AnthropicConfiguration { ApiKey = null! };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AnthropicConfiguration { ApiKey = "" };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_WhitespaceApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AnthropicConfiguration { ApiKey = "   " };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_NullModel_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AnthropicConfiguration
        {
            ApiKey = "test-key",
            Model = null!
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyModel_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AnthropicConfiguration
        {
            ApiKey = "test-key",
            Model = ""
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_WhitespaceModel_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AnthropicConfiguration
        {
            ApiKey = "test-key",
            Model = "   "
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new AnthropicConfiguration
        {
            ApiKey = "test-key",
            Model = "claude-3-5-sonnet-20241022"
        };

        // Act & Assert - Should not throw
        config.Validate();
    }
}
