using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class LLMConfigurationTests
{
    // ===== Multi-Provider Validation Tests =====

    [TestMethod]
    public void Validate_NullProviders_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration { Providers = null };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyProviders_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>()
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_NullActiveProvider_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = null,
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyActiveProvider_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ActiveProviderNotInProviders_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "non-existent",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_DefaultParametersTemperatureBelowZero_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            },
            DefaultParameters = new ProviderParameters(-0.1, null, null)
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_DefaultParametersTemperatureAboveTwo_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            },
            DefaultParameters = new ProviderParameters(2.1, null, null)
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_DefaultParametersTopPBelowZero_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            },
            DefaultParameters = new ProviderParameters(null, -0.1, null)
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_DefaultParametersTopPAboveOne_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            },
            DefaultParameters = new ProviderParameters(null, 1.1, null)
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_DefaultParametersMaxTokensZero_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            },
            DefaultParameters = new ProviderParameters(null, null, 0)
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_DefaultParametersMaxTokensNegative_ThrowsConfigurationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            },
            DefaultParameters = new ProviderParameters(null, null, -1)
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ValidMultiProviderConfig_DoesNotThrow()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = "test",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test"] = new ProviderConfig("Anthropic", "Test", new Dictionary<string, object>())
            },
            DefaultParameters = new ProviderParameters(0.7, 1.0, 4096)
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    // ===== Multi-Provider Configuration Tests =====

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

}
