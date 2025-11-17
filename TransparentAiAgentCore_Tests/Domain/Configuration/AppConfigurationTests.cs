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
                ActiveProvider = "azure-gpt4",
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
                Providers = null // Invalid - no providers
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
                ActiveProvider = "azure-gpt4",
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
