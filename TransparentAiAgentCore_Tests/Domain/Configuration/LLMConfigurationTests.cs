using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class LLMConfigurationTests
{
    [TestMethod]
    public void Validate_NullProvider_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { Provider = null! };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyProvider_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { Provider = "" };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_TemperatureBelowZero_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { Temperature = -0.1 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_TemperatureAboveTwo_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { Temperature = 2.1 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_TopPBelowZero_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { TopP = -0.1 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_TopPAboveOne_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { TopP = 1.1 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_MaxTokensZero_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { MaxTokens = 0 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_MaxTokensNegative_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { MaxTokens = -1 };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_AzureOpenAI_MissingConfig_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Provider = "AzureOpenAI",
            AzureOpenAI = null
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_AzureOpenAI_InvalidConfig_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Provider = "AzureOpenAI",
            AzureOpenAI = new AzureOpenAIConfiguration
            {
                AuthenticationMode = AuthenticationMode.ApiKey,
                Endpoint = "", // Invalid
                ApiKey = "test-key",
                DeploymentName = "gpt-4"
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_AzureOpenAI_ValidConfig_DoesNotThrow()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Provider = "AzureOpenAI",
            AzureOpenAI = new AzureOpenAIConfiguration
            {
                AuthenticationMode = AuthenticationMode.ApiKey,
                Endpoint = "https://test.openai.azure.com",
                ApiKey = "test-key",
                DeploymentName = "gpt-4",
                ApiVersion = "2024-02-15-preview"
            }
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    [TestMethod]
    public void Validate_Anthropic_MissingConfig_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Provider = "Anthropic",
            Anthropic = null
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_Anthropic_InvalidConfig_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Provider = "Anthropic",
            Anthropic = new AnthropicConfiguration
            {
                ApiKey = "", // Invalid
                Model = "claude-3-5-sonnet-20241022"
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_Anthropic_ValidConfig_DoesNotThrow()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Provider = "Anthropic",
            Anthropic = new AnthropicConfiguration
            {
                ApiKey = "test-key",
                Model = "claude-3-5-sonnet-20241022"
            }
        };

        // Act & Assert - Should not throw
        config.Validate();
    }
}
