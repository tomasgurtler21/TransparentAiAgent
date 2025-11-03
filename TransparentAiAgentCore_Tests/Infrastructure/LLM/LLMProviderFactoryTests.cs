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

    [TestMethod]
    public void CreateProvider_NoParameters_UsesConfiguredProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act
        var provider = factory.CreateProvider();

        // Assert
        Assert.IsNotNull(provider);
        Assert.AreEqual("AzureOpenAI", provider.ProviderName);
    }

    [TestMethod]
    public void CreateProvider_NullProviderName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => factory.CreateProvider(null!));
    }

    [TestMethod]
    public void CreateProvider_EmptyProviderName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => factory.CreateProvider(""));
    }

    [TestMethod]
    public void CreateProvider_WhitespaceProviderName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => factory.CreateProvider("   "));
    }

    [TestMethod]
    public void CreateProvider_AzureOpenAI_ReturnsAzureOpenAIProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act
        var provider = factory.CreateProvider("AzureOpenAI");

        // Assert
        Assert.IsNotNull(provider);
        Assert.AreEqual("AzureOpenAI", provider.ProviderName);
        Assert.IsInstanceOfType(provider, typeof(AzureOpenAIProvider));
    }

    [TestMethod]
    public void CreateProvider_CaseInsensitive_ReturnsCorrectProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        var provider1 = factory.CreateProvider("azureopenai");
        Assert.AreEqual("AzureOpenAI", provider1.ProviderName);

        var provider2 = factory.CreateProvider("AZUREOPENAI");
        Assert.AreEqual("AzureOpenAI", provider2.ProviderName);
    }

    [TestMethod]
    public void CreateProvider_Anthropic_ReturnsAnthropicProvider()
    {
        // Arrange
        var config = CreateAnthropicConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act
        var provider = factory.CreateProvider("Anthropic");

        // Assert
        Assert.IsNotNull(provider);
        Assert.AreEqual("Anthropic", provider.ProviderName);
        Assert.IsInstanceOfType(provider, typeof(AnthropicProvider));
    }

    [TestMethod]
    public void CreateProvider_AnthropicCaseInsensitive_ReturnsCorrectProvider()
    {
        // Arrange
        var config = CreateAnthropicConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        var provider1 = factory.CreateProvider("anthropic");
        Assert.AreEqual("Anthropic", provider1.ProviderName);

        var provider2 = factory.CreateProvider("ANTHROPIC");
        Assert.AreEqual("Anthropic", provider2.ProviderName);
    }

    [TestMethod]
    public void CreateProvider_AnthropicNotConfigured_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "Anthropic",
                Anthropic = null // Not configured
            }
        };
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => factory.CreateProvider("Anthropic"));
    }

    [TestMethod]
    public void CreateProvider_UnknownProvider_ThrowsConfigurationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => factory.CreateProvider("UnknownProvider"));
    }

    [TestMethod]
    public void CreateProvider_AzureOpenAINotConfigured_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                AzureOpenAI = null // Not configured
            }
        };
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var factory = new LLMProviderFactory(authProvider, transparencyService, config);

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => factory.CreateProvider("AzureOpenAI"));
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
}
