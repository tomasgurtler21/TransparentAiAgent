using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class AppConfigurationTests
{
    [TestMethod]
    public void Validate_ValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "You are helpful",
                ContextWindowSize = 20
            },
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                AzureOpenAI = new AzureOpenAIConfiguration
                {
                    Endpoint = "https://test.openai.azure.com",
                    ApiKey = "test-key",
                    DeploymentName = "gpt-4",
                    ApiVersion = "2024-02-15-preview"
                }
            },
            MCP = new MCPConfiguration()
        };

        // Act & Assert - Should not throw
        config.Validate();
    }

    [TestMethod]
    public void Validate_InvalidAgentConfig_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "", // Invalid
                ContextWindowSize = 20
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_InvalidLLMConfig_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "You are helpful",
                ContextWindowSize = 20
            },
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                AzureOpenAI = null // Invalid
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }

    [TestMethod]
    public void Validate_InvalidMCPConfig_ThrowsConfigurationException()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "You are helpful",
                ContextWindowSize = 20
            },
            LLM = new LLMConfiguration
            {
                Provider = "AzureOpenAI",
                AzureOpenAI = new AzureOpenAIConfiguration
                {
                    Endpoint = "https://test.openai.azure.com",
                    ApiKey = "test-key",
                    DeploymentName = "gpt-4",
                    ApiVersion = "2024-02-15-preview"
                }
            },
            MCP = new MCPConfiguration
            {
                Servers = new List<MCPServerConfiguration>
                {
                    new MCPServerConfiguration
                    {
                        Name = "", // Invalid
                        Command = "npx"
                    }
                }
            }
        };

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => config.Validate());
    }
}
