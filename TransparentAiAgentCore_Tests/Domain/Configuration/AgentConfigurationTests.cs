using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class AgentConfigurationTests
{
    [TestMethod]
    public void Validate_NullSystemPrompt_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AgentConfiguration { SystemPrompt = null! };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptySystemPrompt_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AgentConfiguration { SystemPrompt = "" };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_WhitespaceSystemPrompt_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AgentConfiguration { SystemPrompt = "   " };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ContextWindowSizeZero_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AgentConfiguration { ContextWindowSize = 0 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ContextWindowSizeNegative_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AgentConfiguration { ContextWindowSize = -1 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            SystemPrompt = "You are a helpful assistant.",
            ContextWindowSize = 20
        };

        // Act & Assert - Should not throw
        config.Validate();
    }
}
