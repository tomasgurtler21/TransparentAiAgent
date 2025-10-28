using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class AzureOpenAIConfigurationTests
{
    [TestMethod]
    public void Validate_NullEndpoint_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration { Endpoint = null! };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyEndpoint_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration { Endpoint = "" };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_WhitespaceEndpoint_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration { Endpoint = "   " };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_NullApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = null!
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = ""
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_NullDeploymentName_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = null!
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyDeploymentName_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = ""
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_NullApiVersion_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = "gpt-4",
            ApiVersion = null!
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_EmptyApiVersion_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = "gpt-4",
            ApiVersion = ""
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_InvalidUriFormat_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "not-a-valid-uri",
            ApiKey = "test-key",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview"
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview"
        };

        // Act & Assert - Should not throw
        config.Validate();
    }
}
