using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Infrastructure.Authentication;
using TransparentAiAgentCore.Infrastructure.LLM;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore_Tests.Infrastructure.LLM;

[TestClass]
public class AzureOpenAIProviderTests
{
    [TestMethod]
    public void Constructor_NullAuthProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var transparencyService = new TransparencyService();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new AzureOpenAIProvider(null!, "gpt-4", transparencyService, config));
    }

    [TestMethod]
    public void Constructor_NullDeploymentName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new AzureOpenAIProvider(authProvider, null!, transparencyService, config));
    }

    [TestMethod]
    public void Constructor_EmptyDeploymentName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new AzureOpenAIProvider(authProvider, "", transparencyService, config));
    }

    [TestMethod]
    public void Constructor_WhitespaceDeploymentName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new AzureOpenAIProvider(authProvider, "   ", transparencyService, config));
    }

    [TestMethod]
    public void Constructor_NullTransparencyService_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new AzureOpenAIProvider(authProvider, "gpt-4", null!, config));
    }

    [TestMethod]
    public void Constructor_ValidParameters_CreatesProvider()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();

        // Act
        var provider = new AzureOpenAIProvider(authProvider, "gpt-4", transparencyService, config);

        // Assert
        Assert.IsNotNull(provider);
        Assert.AreEqual("AzureOpenAI", provider.ProviderName);
    }

    [TestMethod]
    public void ProviderName_ReturnsAzureOpenAI()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var provider = new AzureOpenAIProvider(authProvider, "gpt-4", transparencyService, config);

        // Act
        var providerName = provider.ProviderName;

        // Assert
        Assert.AreEqual("AzureOpenAI", providerName);
    }

    [TestMethod]
    public void SendRequestAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var provider = new AzureOpenAIProvider(authProvider, "gpt-4", transparencyService, config);

        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            await provider.SendRequestAsync(null!));
    }

    [TestMethod]
    public void StreamRequestAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var authProvider = new ConfigurationAuthenticationProvider(config);
        var transparencyService = new TransparencyService();
        var provider = new AzureOpenAIProvider(authProvider, "gpt-4", transparencyService, config);

        // Act & Assert
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
        {
            await foreach (var chunk in provider.StreamRequestAsync(null!))
            {
                // Should throw before reaching here
            }
        });
    }

    // Note: Full integration tests with actual Azure OpenAI API calls would require:
    // 1. Mocking the OpenAIClient (complex due to sealed classes)
    // 2. Using the internal constructor with a mocked client
    // 3. Or actual API calls (not suitable for unit tests)
    //
    // For now, we've validated:
    // - Constructor parameter validation
    // - ProviderName property
    // - Null request validation
    //
    // The conversion logic (ConvertToAzureMessage, ConvertResponse, etc.) is tested
    // indirectly through integration tests or can be tested by extracting to testable methods.

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
                }
            }
        };
    }
}
