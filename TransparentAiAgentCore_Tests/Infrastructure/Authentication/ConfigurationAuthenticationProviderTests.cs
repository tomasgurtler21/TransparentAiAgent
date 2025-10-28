using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Authentication;

namespace TransparentAiAgentCore_Tests.Infrastructure.Authentication;

[TestClass]
public class ConfigurationAuthenticationProviderTests
{
    [TestMethod]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new ConfigurationAuthenticationProvider(null!));
    }

    [TestMethod]
    public void GetApiKey_NullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => provider.GetApiKey(null!));
    }

    [TestMethod]
    public void GetApiKey_EmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => provider.GetApiKey(""));
    }

    [TestMethod]
    public void GetApiKey_WhitespaceServiceName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => provider.GetApiKey("   "));
    }

    [TestMethod]
    public void GetApiKey_AzureOpenAI_ReturnsCorrectApiKey()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act
        var apiKey = provider.GetApiKey("AzureOpenAI");

        // Assert
        Assert.AreEqual("test-azure-key", apiKey);
    }

    [TestMethod]
    public void GetApiKey_Anthropic_ReturnsCorrectApiKey()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act
        var apiKey = provider.GetApiKey("Anthropic");

        // Assert
        Assert.AreEqual("test-anthropic-key", apiKey);
    }

    [TestMethod]
    public void GetApiKey_CaseInsensitive_ReturnsCorrectApiKey()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.AreEqual("test-azure-key", provider.GetApiKey("azureopenai"));
        Assert.AreEqual("test-azure-key", provider.GetApiKey("AZUREOPENAI"));
        Assert.AreEqual("test-anthropic-key", provider.GetApiKey("anthropic"));
        Assert.AreEqual("test-anthropic-key", provider.GetApiKey("ANTHROPIC"));
    }

    [TestMethod]
    public void GetApiKey_AzureOpenAINotConfigured_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                AzureOpenAI = null, // Not configured
                Anthropic = new AnthropicConfiguration
                {
                    ApiKey = "test-anthropic-key"
                }
            }
        };
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => provider.GetApiKey("AzureOpenAI"));
    }

    [TestMethod]
    public void GetApiKey_AnthropicNotConfigured_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AppConfiguration
        {
            LLM = new LLMConfiguration
            {
                Provider = "Anthropic",
                AzureOpenAI = new AzureOpenAIConfiguration
                {
                    ApiKey = "test-azure-key",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4"
                },
                Anthropic = null // Not configured
            }
        };
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => provider.GetApiKey("Anthropic"));
    }

    [TestMethod]
    public void GetApiKey_UnknownService_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => provider.GetApiKey("UnknownService"));
    }

    [TestMethod]
    public void GetEndpoint_NullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => provider.GetEndpoint(null!));
    }

    [TestMethod]
    public void GetEndpoint_EmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => provider.GetEndpoint(""));
    }

    [TestMethod]
    public void GetEndpoint_AzureOpenAI_ReturnsCorrectEndpoint()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act
        var endpoint = provider.GetEndpoint("AzureOpenAI");

        // Assert
        Assert.AreEqual("https://test.openai.azure.com", endpoint);
    }

    [TestMethod]
    public void GetEndpoint_Anthropic_ReturnsFixedEndpoint()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act
        var endpoint = provider.GetEndpoint("Anthropic");

        // Assert
        Assert.AreEqual("https://api.anthropic.com", endpoint);
    }

    [TestMethod]
    public void GetEndpoint_CaseInsensitive_ReturnsCorrectEndpoint()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.AreEqual("https://test.openai.azure.com", provider.GetEndpoint("azureopenai"));
        Assert.AreEqual("https://test.openai.azure.com", provider.GetEndpoint("AZUREOPENAI"));
        Assert.AreEqual("https://api.anthropic.com", provider.GetEndpoint("anthropic"));
        Assert.AreEqual("https://api.anthropic.com", provider.GetEndpoint("ANTHROPIC"));
    }

    [TestMethod]
    public void GetEndpoint_AzureOpenAINotConfigured_ThrowsConfigurationException()
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
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => provider.GetEndpoint("AzureOpenAI"));
    }

    [TestMethod]
    public void GetEndpoint_UnknownService_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var provider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => provider.GetEndpoint("UnknownService"));
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
                    ApiKey = "test-azure-key",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4"
                },
                Anthropic = new AnthropicConfiguration
                {
                    ApiKey = "test-anthropic-key"
                }
            }
        };
    }
}
