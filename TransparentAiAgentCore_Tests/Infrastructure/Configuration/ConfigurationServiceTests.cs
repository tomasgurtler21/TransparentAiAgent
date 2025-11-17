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
                ActiveProvider = "claude-fast",
                DefaultParameters = new ProviderParameters(0.8, null, null),
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["claude-fast"] = new ProviderConfig(
                        type: "Anthropic",
                        displayName: "Claude Fast",
                        parameters: new Dictionary<string, object>
                        {
                            ["Model"] = "claude-3-5-sonnet-20241022",
                            ["ApiKey"] = "test-key"
                        }
                    )
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
        Assert.AreEqual("claude-fast", retrieved.LLM.ActiveProvider);
        Assert.IsTrue(retrieved.LLM.Providers.ContainsKey("claude-fast"));
        Assert.AreEqual(0.8, retrieved.LLM.DefaultParameters?.Temperature);
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
                ActiveProvider = "azure-gpt4",
                DefaultParameters = new ProviderParameters(0.7, 1.0, 1000),
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["azure-gpt4"] = new ProviderConfig(
                        type: "AzureOpenAI",
                        displayName: "Azure GPT-4",
                        parameters: new Dictionary<string, object>
                        {
                            ["Endpoint"] = "https://test.openai.azure.com",
                            ["ApiKey"] = "test-key",
                            ["DeploymentName"] = "gpt-4",
                            ["ApiVersion"] = "2024-02-15-preview",
                            ["AuthenticationMode"] = "ApiKey"
                        }
                    )
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

    // ===== Step 6: Configuration Persistence Tests =====

    [TestMethod]
    public async Task UpdateActiveProviderAsync_ValidProvider_UpdatesConfiguration()
    {
        // Arrange
        var testFilePath = Path.GetTempFileName();

        // Set up configuration with multiple providers
        var config = CreateMultiProviderConfiguration();
        _service.UpdateConfiguration(config);

        try
        {
            // Act
            var updatedConfig = await _service.UpdateActiveProviderAsync("azure-gpt4", testFilePath);

            // Assert
            Assert.IsNotNull(updatedConfig);
            Assert.AreEqual("azure-gpt4", updatedConfig.LLM.ActiveProvider);
            Assert.AreEqual("azure-gpt4", _service.GetConfiguration().LLM.ActiveProvider);

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
    public async Task UpdateActiveProviderAsync_NullProviderName_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.UpdateActiveProviderAsync(null!));
    }

    [TestMethod]
    public async Task UpdateActiveProviderAsync_EmptyProviderName_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.UpdateActiveProviderAsync(string.Empty));
    }

    [TestMethod]
    public async Task UpdateActiveProviderAsync_WhitespaceProviderName_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.UpdateActiveProviderAsync("   "));
    }

    [TestMethod]
    public async Task UpdateActiveProviderAsync_NonexistentProvider_ThrowsConfigurationException()
    {
        // Arrange
        var config = CreateMultiProviderConfiguration();
        _service.UpdateConfiguration(config);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ConfigurationException>(
            () => _service.UpdateActiveProviderAsync("nonexistent-provider"));
    }

    [TestMethod]
    public async Task UpdateActiveProviderAsync_NoProvidersConfigured_ThrowsConfigurationException()
    {
        // Arrange - configuration with empty Providers dictionary should fail validation
        var config = CreateMultiProviderConfiguration();
        config.LLM.Providers = new Dictionary<string, ProviderConfig>(); // Empty providers dictionary

        // Act & Assert - should throw during UpdateConfiguration (validation failure)
        Assert.ThrowsException<ConfigurationException>(() =>
            _service.UpdateConfiguration(config));
    }

    private AppConfiguration CreateMultiProviderConfiguration()
    {
        return new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "Test system prompt",
                ContextWindowSize = 20
            },
            LLM = new LLMConfiguration
            {
                ActiveProvider = "claude-fast",
                DefaultParameters = new ProviderParameters(0.7, 1.0, 4096),
                Providers = new Dictionary<string, ProviderConfig>
                {
                    ["claude-fast"] = new ProviderConfig(
                        type: "Anthropic",
                        displayName: "Claude Fast",
                        parameters: new Dictionary<string, object>
                        {
                            ["Model"] = "claude-haiku-4-5-20251001",
                            ["ApiKey"] = "sk-ant-test-key"
                        }
                    ),
                    ["azure-gpt4"] = new ProviderConfig(
                        type: "AzureOpenAI",
                        displayName: "Azure GPT-4",
                        parameters: new Dictionary<string, object>
                        {
                            ["Endpoint"] = "https://test.openai.azure.com/",
                            ["DeploymentName"] = "gpt-4",
                            ["ApiKey"] = "test-key",
                            ["ApiVersion"] = "2024-02-15-preview",
                            ["AuthenticationMode"] = "ApiKey"
                        }
                    )
                }
            },
            MCP = new MCPConfiguration
            {
                AutoDiscoverTools = true
            }
        };
    }
}
