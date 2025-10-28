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
}
