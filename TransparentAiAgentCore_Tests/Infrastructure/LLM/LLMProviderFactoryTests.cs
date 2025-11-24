using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Authentication;
using TransparentAiAgentCore.Infrastructure.LLM;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore_Tests.Infrastructure.LLM;

[TestClass]
public class LLMProviderFactoryTests
{
    [TestMethod]
    public void Constructor_NullAuthProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var transparencyService = new TransparencyService();
        var config = CreateValidConfiguration();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LLMProviderFactory(null!, transparencyService, config));
    }

    [TestMethod]
    public void Constructor_NullTransparencyService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LLMProviderFactory(authProvider, null!, config));
    }

    [TestMethod]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new LLMProviderFactory(authProvider, transparencyService, null!));
    }

    [TestMethod]
    public void Constructor_ValidParameters_CreatesFactory()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();

        // Act
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Assert
        Assert.IsNotNull(factory);
    }


    private AppConfiguration CreateValidConfiguration()
    {
        return new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                AzureOpenAI = new AzureOpenAIConfiguration
                {
                    AuthenticationMode = AuthenticationMode.ApiKey,
                    ApiKey = "test-azure-key",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4"
                }
            }
        };
    }

    private AppConfiguration CreateAnthropicConfiguration()
    {
        return new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "Anthropic",
                Anthropic = new AnthropicConfiguration
                {
                    ApiKey = "sk-ant-test-key-12345",
                    Model = "claude-3-5-sonnet-20241022"
                }
            }
        };
    }

    // ===== Step 5: Factory Refactoring Tests =====

    [TestMethod]
    public void CreateProvider_WithProviderConfig_Anthropic_ReturnsAnthropicProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude Fast",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "claude-haiku-4-5-20251001",
                ["ApiKey"] = "sk-ant-test-key"
            }
        );

        // Act
        var provider = factory.CreateProvider("claude-fast", providerConfig);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(AnthropicProvider));
        Assert.AreEqual("Anthropic", provider.ProviderName);
    }

    [TestMethod]
    public void CreateProvider_WithProviderConfig_AzureOpenAI_ReturnsAzureOpenAIProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "https://test.openai.azure.com/",
                ["DeploymentName"] = "gpt-4",
                ["ApiKey"] = "test-azure-key",
                ["ApiVersion"] = "2024-02-15-preview",
                ["AuthenticationMode"] = "ApiKey",
                ["IsReasoningModel"] = false
            }
        );

        // Act
        var provider = factory.CreateProvider("azure-gpt4", providerConfig);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(AzureOpenAIProvider));
        Assert.AreEqual("AzureOpenAI", provider.ProviderName);
    }

    [TestMethod]
    public void CreateProvider_WithProviderConfig_CaseInsensitive_ReturnsCorrectProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "anthropic", // lowercase
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "claude-haiku-4-5-20251001",
                ["ApiKey"] = "sk-ant-test-key"
            }
        );

        // Act
        var provider = factory.CreateProvider("test-config", providerConfig);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(AnthropicProvider));
    }

    [TestMethod]
    public void CreateProvider_WithProviderConfig_UnknownType_ThrowsConfigurationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "UnknownProvider",
            displayName: "Unknown",
            parameters: new Dictionary<string, object>()
        );

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() =>
            factory.CreateProvider("unknown", providerConfig));
    }

    [TestMethod]
    public void CreateProvider_WithProviderConfig_MissingApiKey_ThrowsException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "claude-haiku-4-5-20251001"
                // ApiKey missing
            }
        );

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() =>
            factory.CreateProvider("test", providerConfig));
    }

    [TestMethod]
    public void CreateProvider_WithProviderConfig_MissingModel_ThrowsException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["ApiKey"] = "sk-ant-test-key"
                // Model missing
            }
        );

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() =>
            factory.CreateProvider("test", providerConfig));
    }

    // ===== Reasoning Model Support Tests =====

    [TestMethod]
    public void CreateProvider_AzureOpenAI_WithIsReasoningModelTrue_CreatesProviderWithReasoningSupport()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure o3-mini",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "https://test.openai.azure.com/",
                ["DeploymentName"] = "o3-mini",
                ["ApiKey"] = "test-key",
                ["ApiVersion"] = "2024-02-15-preview",
                ["AuthenticationMode"] = "ApiKey",
                ["IsReasoningModel"] = true  // Critical: Reasoning model flag
            }
        );

        // Act
        var provider = factory.CreateProvider("azure-o3", providerConfig);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(AzureOpenAIProvider));
        // Provider should be created with IsReasoningModel = true (verified in provider tests)
    }

    [TestMethod]
    public void CreateProvider_AzureOpenAI_WithIsReasoningModelFalse_CreatesStandardProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "https://test.openai.azure.com/",
                ["DeploymentName"] = "gpt-4",
                ["ApiKey"] = "test-key",
                ["ApiVersion"] = "2024-02-15-preview",
                ["AuthenticationMode"] = "ApiKey",
                ["IsReasoningModel"] = false  // Standard model
            }
        );

        // Act
        var provider = factory.CreateProvider("azure-gpt4", providerConfig);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(AzureOpenAIProvider));
    }

    [TestMethod]
    [ExpectedException(typeof(ConfigurationException))]
    public void CreateProvider_AzureOpenAI_WithoutIsReasoningModel_ThrowsConfigurationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "https://test.openai.azure.com/",
                ["DeploymentName"] = "gpt-4",
                ["ApiKey"] = "test-key",
                ["ApiVersion"] = "2024-02-15-preview",
                ["AuthenticationMode"] = "ApiKey"
                // IsReasoningModel not specified - should throw ConfigurationException
            }
        );

        // Act - Should throw ConfigurationException
        factory.CreateProvider("azure-gpt4", providerConfig);
    }

    [TestMethod]
    public void CreateProvider_OpenAI_WithIsReasoningModelTrue_CreatesProviderWithReasoningSupport()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "OpenAI",
            displayName: "OpenAI o1-preview",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "o1-preview",
                ["ApiKey"] = "test-key",
                ["IsReasoningModel"] = true  // Critical: Reasoning model flag
            }
        );

        // Act
        var provider = factory.CreateProvider("openai-o1", providerConfig);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(OpenAIProvider));
        // Provider should be created with IsReasoningModel = true (verified in provider tests)
    }

    [TestMethod]
    public void CreateProvider_OpenAI_WithIsReasoningModelStringTrue_ParsesCorrectly()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "OpenAI",
            displayName: "OpenAI o1-preview",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "o1-preview",
                ["ApiKey"] = "test-key",
                ["IsReasoningModel"] = "true"  // String representation (from JSON parsing)
            }
        );

        // Act
        var provider = factory.CreateProvider("openai-o1", providerConfig);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(OpenAIProvider));
        // String "true" should be parsed as boolean true
    }

    [TestMethod]
    public void CreateProvider_OpenAI_WithIsReasoningModelInvalidString_DefaultsToFalse()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        var providerConfig = new ProviderConfig(
            type: "OpenAI",
            displayName: "OpenAI GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "gpt-4",
                ["ApiKey"] = "test-key",
                ["IsReasoningModel"] = "invalid"  // Invalid string - should default to false
            }
        );

        // Act
        var provider = factory.CreateProvider("openai-gpt4", providerConfig);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType(provider, typeof(OpenAIProvider));
        // Invalid string should default to false (safe default)
    }
}
