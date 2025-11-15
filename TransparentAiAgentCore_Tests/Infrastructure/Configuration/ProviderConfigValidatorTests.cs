using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Configuration;

namespace TransparentAiAgentCore_Tests.Infrastructure.Configuration;

/// <summary>
/// Tests for ProviderConfigValidator - validates provider configuration structure
/// </summary>
[TestClass]
public class ProviderConfigValidatorTests
{
    #region Anthropic Provider Tests

    [TestMethod]
    public void ValidateAnthropicConfig_ValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "claude-haiku-4-5-20251001",
                ["ApiKey"] = "sk-ant-test123"
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsTrue(result.IsValid, "Valid Anthropic config should pass validation");
    }

    [TestMethod]
    public void ValidateAnthropicConfig_MissingModel_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["ApiKey"] = "sk-ant-test123"
                // Model missing
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "Anthropic config without Model should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Model")),
            "Error should mention missing Model parameter");
    }

    [TestMethod]
    public void ValidateAnthropicConfig_MissingApiKey_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "claude-haiku-4-5-20251001"
                // ApiKey missing
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "Anthropic config without ApiKey should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ApiKey")),
            "Error should mention missing ApiKey parameter");
    }

    [TestMethod]
    public void ValidateAnthropicConfig_EmptyApiKey_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "Anthropic",
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "claude-haiku-4-5-20251001",
                ["ApiKey"] = ""  // Empty string
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "Anthropic config with empty ApiKey should fail validation");
    }

    #endregion

    #region Azure OpenAI Provider Tests

    [TestMethod]
    public void ValidateAzureOpenAIConfig_ValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "https://test.openai.azure.com/",
                ["DeploymentName"] = "gpt-4",
                ["ApiKey"] = "test-key",
                ["ApiVersion"] = "2024-02-15-preview"
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsTrue(result.IsValid, "Valid Azure OpenAI config should pass validation");
    }

    [TestMethod]
    public void ValidateAzureOpenAIConfig_InvalidEndpoint_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "not-a-valid-url",  // Invalid URL
                ["DeploymentName"] = "gpt-4",
                ["ApiKey"] = "test-key"
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "Azure OpenAI config with invalid endpoint should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Endpoint")),
            "Error should mention invalid Endpoint");
    }

    [TestMethod]
    public void ValidateAzureOpenAIConfig_MissingEndpoint_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["DeploymentName"] = "gpt-4",
                ["ApiKey"] = "test-key"
                // Endpoint missing
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "Azure OpenAI config without Endpoint should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Endpoint")),
            "Error should mention missing Endpoint");
    }

    [TestMethod]
    public void ValidateAzureOpenAIConfig_MissingDeploymentName_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "https://test.openai.azure.com/",
                ["ApiKey"] = "test-key"
                // DeploymentName missing
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "Azure OpenAI config without DeploymentName should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("DeploymentName")),
            "Error should mention missing DeploymentName");
    }

    [TestMethod]
    public void ValidateAzureOpenAIConfig_MissingApiKey_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "AzureOpenAI",
            displayName: "Azure GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Endpoint"] = "https://test.openai.azure.com/",
                ["DeploymentName"] = "gpt-4"
                // ApiKey missing
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "Azure OpenAI config without ApiKey should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ApiKey")),
            "Error should mention missing ApiKey");
    }

    #endregion

    #region OpenAI Provider Tests

    [TestMethod]
    public void ValidateOpenAIConfig_ValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "OpenAI",
            displayName: "GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "gpt-4o",
                ["ApiKey"] = "sk-test123"
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsTrue(result.IsValid, "Valid OpenAI config should pass validation");
    }

    [TestMethod]
    public void ValidateOpenAIConfig_MissingModel_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "OpenAI",
            displayName: "GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["ApiKey"] = "sk-test123"
                // Model missing
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "OpenAI config without Model should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Model")),
            "Error should mention missing Model");
    }

    [TestMethod]
    public void ValidateOpenAIConfig_MissingApiKey_ReturnsError()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "OpenAI",
            displayName: "GPT-4",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "gpt-4o"
                // ApiKey missing
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid, "OpenAI config without ApiKey should fail validation");
        Assert.IsTrue(result.Errors.Any(e => e.Contains("ApiKey")),
            "Error should mention missing ApiKey");
    }

    #endregion

    #region Unknown Provider Tests

    [TestMethod]
    public void Validate_UnknownProviderType_ThrowsConfigurationException()
    {
        // Arrange
        var config = new ProviderConfig(
            type: "UnknownProvider",
            displayName: "Unknown",
            parameters: new Dictionary<string, object>()
        );

        var validator = new ProviderConfigValidator();

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() =>
            validator.Validate(config),
            "Unknown provider type should throw ConfigurationException");
    }

    [TestMethod]
    public void Validate_CaseInsensitiveProviderType_WorksCorrectly()
    {
        // Arrange - Test case insensitivity
        var config = new ProviderConfig(
            type: "anthropic",  // lowercase
            displayName: "Claude",
            parameters: new Dictionary<string, object>
            {
                ["Model"] = "claude-haiku-4-5-20251001",
                ["ApiKey"] = "sk-ant-test123"
            }
        );

        var validator = new ProviderConfigValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.IsTrue(result.IsValid, "Provider type should be case-insensitive");
    }

    #endregion
}
