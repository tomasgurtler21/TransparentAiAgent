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

    // New tests for multi-provider configuration

    [TestMethod]
    public void LLMConfiguration_MultipleProviders_LoadsSuccessfully()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "claude-fast",
            DefaultParameters = new ProviderParameters(0.7, 1.0, 4096),
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["claude-fast"] = new ProviderConfig("Anthropic", "Claude Fast", new Dictionary<string, object>()),
                ["azure-gpt4"] = new ProviderConfig("AzureOpenAI", "GPT-4 Azure", new Dictionary<string, object>())
            }
        };

        // Assert
        Assert.AreEqual("claude-fast", config.ActiveProvider);
        Assert.AreEqual(2, config.Providers.Count);
        Assert.IsTrue(config.Providers.ContainsKey("claude-fast"));
        Assert.IsTrue(config.Providers.ContainsKey("azure-gpt4"));
    }

    [TestMethod]
    public void LLMConfiguration_DefaultParameters_SetsCorrectly()
    {
        // Arrange
        var defaultParams = new ProviderParameters(0.7, 1.0, 4096);
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            DefaultParameters = defaultParams,
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            }
        };

        // Assert
        Assert.IsNotNull(config.DefaultParameters);
        Assert.AreEqual(0.7, config.DefaultParameters.Temperature);
        Assert.AreEqual(1.0, config.DefaultParameters.TopP);
        Assert.AreEqual(4096, config.DefaultParameters.MaxTokens);
    }

    [TestMethod]
    public void LLMConfiguration_NullProviders_AllowsNullForBackwardCompatibility()
    {
        // Arrange & Act
        var config = new LLMConfiguration
        {
            Providers = null
        };

        // Assert - Should not throw, allows null for backward compatibility
        Assert.IsNull(config.Providers);
    }

    [TestMethod]
    public void LLMConfiguration_EmptyProviders_AllowsEmptyDictionary()
    {
        // Arrange & Act
        var config = new LLMConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>()
        };

        // Assert
        Assert.IsNotNull(config.Providers);
        Assert.AreEqual(0, config.Providers.Count);
    }
}
