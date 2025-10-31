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
    public void Validate_UnspecifiedAuthenticationMode_ThrowsConfigurationException()
    {
        // Arrange - AuthenticationMode defaults to Unspecified
        var config = new AzureOpenAIConfiguration
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview"
            // AuthenticationMode not specified (defaults to Unspecified)
        };

        // Act & Assert - Should throw because AuthenticationMode must be explicit
        var exception = Assert.ThrowsException<ConfigurationException>(() => config.Validate());
        Assert.IsTrue(exception.Message.Contains("AuthenticationMode must be explicitly specified"));
    }

    [TestMethod]
    public void Validate_NullApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            AuthenticationMode = AuthenticationMode.ApiKey,
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
            AuthenticationMode = AuthenticationMode.ApiKey,
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
            AuthenticationMode = AuthenticationMode.ApiKey,
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
            AuthenticationMode = AuthenticationMode.ApiKey,
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
            AuthenticationMode = AuthenticationMode.ApiKey,
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
            AuthenticationMode = AuthenticationMode.ApiKey,
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
            AuthenticationMode = AuthenticationMode.ApiKey,
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
        // Arrange - Must explicitly set AuthenticationMode
        var config = new AzureOpenAIConfiguration
        {
            AuthenticationMode = AuthenticationMode.ApiKey,
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview"
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    #region OAuth / DefaultAzureCredential Tests

    [TestMethod]
    public void Validate_DefaultAzureCredentialWithoutApiKey_DoesNotThrow()
    {
        // Arrange - OAuth mode doesn't require ApiKey
        var config = new AzureOpenAIConfiguration
        {
            AuthenticationMode = AuthenticationMode.DefaultAzureCredential,
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview"
            // ApiKey intentionally omitted
        };

        // Act & Assert - Should NOT throw because OAuth doesn't require ApiKey
        config.Validate();
    }

    [TestMethod]
    public void Validate_DefaultAzureCredentialWithTenantId_DoesNotThrow()
    {
        // Arrange
        var config = new AzureOpenAIConfiguration
        {
            AuthenticationMode = AuthenticationMode.DefaultAzureCredential,
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview",
            TenantId = "12345678-1234-1234-1234-123456789012"
        };

        // Act & Assert
        config.Validate();
    }

    [TestMethod]
    public void Validate_ApiKeyModeWithoutApiKey_ThrowsConfigurationException()
    {
        // Arrange - ApiKey mode requires ApiKey
        var config = new AzureOpenAIConfiguration
        {
            AuthenticationMode = AuthenticationMode.ApiKey,
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-15-preview"
            // ApiKey intentionally omitted
        };

        // Act & Assert - Should throw because ApiKey mode requires ApiKey
        var exception = Assert.ThrowsException<ConfigurationException>(() => config.Validate());
        Assert.IsTrue(exception.Message.Contains("ApiKey is required when AuthenticationMode is ApiKey"));
    }

    [TestMethod]
    public void DefaultAuthenticationMode_ShouldBeUnspecified()
    {
        // Arrange & Act
        var config = new AzureOpenAIConfiguration();

        // Assert - Default should be Unspecified to force explicit configuration
        Assert.AreEqual(AuthenticationMode.Unspecified, config.AuthenticationMode);
    }

    #endregion
}
