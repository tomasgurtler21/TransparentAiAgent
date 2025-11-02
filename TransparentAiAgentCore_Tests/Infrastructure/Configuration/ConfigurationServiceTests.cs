using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Configuration;

namespace TransparentAiAgentCore_Tests.Infrastructure.Configuration;

[TestClass]
public class ConfigurationServiceTests
{
    private ConfigurationService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new ConfigurationService();
    }

    [TestMethod]
    public void GetConfiguration_ReturnsCurrentConfiguration()
    {
        // Act
        var config = _service.GetConfiguration();

        // Assert
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.Agent);
        Assert.IsNotNull(config.LLM);
        Assert.IsNotNull(config.MCP);
    }

    [TestMethod]
    public void UpdateConfiguration_ValidConfig_UpdatesCurrentConfiguration()
    {
        // Arrange
        var newConfig = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "Custom prompt",
                ContextWindowSize = 50
            },
            LLM = new LLMConfiguration
            {
                Provider = "Anthropic",
                Temperature = 0.8,
                Anthropic = new AnthropicConfiguration
                {
                    ApiKey = "test-key",
                    Model = "claude-3-5-sonnet-20241022"
                }
            }
        };

        // Act
        _service.UpdateConfiguration(newConfig);
        var retrieved = _service.GetConfiguration();

        // Assert
        Assert.AreSame(newConfig, retrieved);
        Assert.AreEqual("Custom prompt", retrieved.Agent.SystemPrompt);
        Assert.AreEqual(50, retrieved.Agent.ContextWindowSize);
        Assert.AreEqual("Anthropic", retrieved.LLM.Provider);
        Assert.AreEqual(0.8, retrieved.LLM.Temperature);
    }

    [TestMethod]
    public void UpdateConfiguration_InvalidConfig_ThrowsConfigurationException()
    {
        // Arrange
        var invalidConfig = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "", // Invalid
                ContextWindowSize = 20
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => _service.UpdateConfiguration(invalidConfig));
    }

    [TestMethod]
    public void UpdateConfiguration_DoesNotUpdateOnValidationFailure()
    {
        // Arrange
        var originalConfig = _service.GetConfiguration();
        var invalidConfig = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "", // Invalid
                ContextWindowSize = 20
            }
        };

        // Act
        try
        {
            _service.UpdateConfiguration(invalidConfig);
        }
        catch (ConfigurationException)
        {
            // Expected
        }

        // Assert - Original configuration should be unchanged
        var currentConfig = _service.GetConfiguration();
        Assert.AreSame(originalConfig, currentConfig);
    }

    [TestMethod]
    public async Task UpdateSystemPromptAsync_ValidPrompt_UpdatesConfiguration()
    {
        // Arrange
        var testFilePath = Path.GetTempFileName();
        var newPrompt = "Updated system prompt for testing";

        // Set up valid configuration first
        var validConfig = CreateValidConfiguration();
        _service.UpdateConfiguration(validConfig);

        try
        {
            // Act
            var updatedConfig = await _service.UpdateSystemPromptAsync(newPrompt, testFilePath);

            // Assert
            Assert.IsNotNull(updatedConfig);
            Assert.AreEqual(newPrompt, updatedConfig.Agent.SystemPrompt);
            Assert.AreEqual(newPrompt, _service.GetConfiguration().Agent.SystemPrompt);

            // Verify it was saved to file
            Assert.IsTrue(File.Exists(testFilePath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }

    private AppConfiguration CreateValidConfiguration()
    {
        return new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "Default system prompt",
                ContextWindowSize = 20
            },
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                Temperature = 0.7,
                MaxTokens = 1000,
                TopP = 1.0,
                AzureOpenAI = new AzureOpenAIConfiguration
                {
                    AuthenticationMode = AuthenticationMode.ApiKey,
                    ApiKey = "test-key",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4"
                }
            },
            MCP = new MCPConfiguration
            {
                AutoDiscoverTools = true
            }
        };
    }

    [TestMethod]
    public async Task UpdateSystemPromptAsync_NullPrompt_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.UpdateSystemPromptAsync(null!));
    }

    [TestMethod]
    public async Task UpdateSystemPromptAsync_EmptyPrompt_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.UpdateSystemPromptAsync(string.Empty));
    }

    [TestMethod]
    public async Task UpdateLLMParametersAsync_ValidParameters_UpdatesConfiguration()
    {
        // Arrange
        var testFilePath = Path.GetTempFileName();
        var temperature = 0.9;
        var maxTokens = 2000;
        var topP = 0.95;

        // Set up valid configuration first
        var validConfig = CreateValidConfiguration();
        _service.UpdateConfiguration(validConfig);

        try
        {
            // Act
            var updatedConfig = await _service.UpdateLLMParametersAsync(temperature, maxTokens, topP, testFilePath);

            // Assert
            Assert.IsNotNull(updatedConfig);
            Assert.AreEqual(temperature, updatedConfig.LLM.Temperature);
            Assert.AreEqual(maxTokens, updatedConfig.LLM.MaxTokens);
            Assert.AreEqual(topP, updatedConfig.LLM.TopP);

            // Verify current configuration is also updated
            var currentConfig = _service.GetConfiguration();
            Assert.AreEqual(temperature, currentConfig.LLM.Temperature);
            Assert.AreEqual(maxTokens, currentConfig.LLM.MaxTokens);
            Assert.AreEqual(topP, currentConfig.LLM.TopP);

            // Verify it was saved to file
            Assert.IsTrue(File.Exists(testFilePath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }

    [TestMethod]
    public async Task UpdateLLMParametersAsync_InvalidTemperature_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateLLMParametersAsync(2.5, 1000, 1.0));
    }

    [TestMethod]
    public async Task UpdateLLMParametersAsync_NegativeMaxTokens_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateLLMParametersAsync(0.7, -100, 1.0));
    }

    [TestMethod]
    public async Task UpdateLLMParametersAsync_InvalidTopP_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateLLMParametersAsync(0.7, 1000, 1.5));
    }
}
