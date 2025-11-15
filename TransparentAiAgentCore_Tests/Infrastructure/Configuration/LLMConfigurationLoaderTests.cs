using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Configuration;

namespace TransparentAiAgentCore_Tests.Infrastructure.Configuration;

[TestClass]
public class LLMConfigurationLoaderTests
{
    [TestMethod]
    public void LoadFromJson_ValidMultiProviderConfig_LoadsSuccessfully()
    {
        // Arrange: Create test config JSON
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""LLM"": {
                    ""ActiveProvider"": ""claude-fast"",
                    ""DefaultParameters"": {
                        ""Temperature"": 0.7,
                        ""TopP"": 1.0,
                        ""MaxTokens"": 4096
                    },
                    ""Providers"": {
                        ""claude-fast"": {
                            ""Type"": ""Anthropic"",
                            ""DisplayName"": ""Claude Haiku Fast"",
                            ""Parameters"": {
                                ""Model"": ""claude-haiku-4-5-20251001"",
                                ""ApiKey"": ""sk-ant-test""
                            }
                        }
                    }
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act
        var config = loader.LoadFromJson(configJson);

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("claude-fast", config.ActiveProvider);
        Assert.IsNotNull(config.Providers);
        Assert.AreEqual(1, config.Providers.Count);
        Assert.IsTrue(config.Providers.ContainsKey("claude-fast"));
        Assert.AreEqual("Anthropic", config.Providers["claude-fast"].Type);
    }

    [TestMethod]
    public void LoadFromJson_MultipleProviders_LoadsAll()
    {
        // Arrange
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""LLM"": {
                    ""ActiveProvider"": ""claude-fast"",
                    ""DefaultParameters"": {
                        ""Temperature"": 0.7,
                        ""TopP"": 1.0,
                        ""MaxTokens"": 4096
                    },
                    ""Providers"": {
                        ""claude-fast"": {
                            ""Type"": ""Anthropic"",
                            ""DisplayName"": ""Claude Haiku Fast"",
                            ""Parameters"": {
                                ""Model"": ""claude-haiku-4-5-20251001"",
                                ""ApiKey"": ""sk-ant-test""
                            }
                        },
                        ""azure-gpt4"": {
                            ""Type"": ""AzureOpenAI"",
                            ""DisplayName"": ""Azure GPT-4"",
                            ""Parameters"": {
                                ""Endpoint"": ""https://test.openai.azure.com/"",
                                ""DeploymentName"": ""gpt-4"",
                                ""ApiKey"": ""test-key""
                            }
                        }
                    }
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act
        var config = loader.LoadFromJson(configJson);

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("claude-fast", config.ActiveProvider);
        Assert.AreEqual(2, config.Providers.Count);
        Assert.IsTrue(config.Providers.ContainsKey("claude-fast"));
        Assert.IsTrue(config.Providers.ContainsKey("azure-gpt4"));
    }

    [TestMethod]
    public void LoadFromJson_WithDefaultParameters_LoadsCorrectly()
    {
        // Arrange
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""LLM"": {
                    ""ActiveProvider"": ""claude-fast"",
                    ""DefaultParameters"": {
                        ""Temperature"": 0.9,
                        ""TopP"": 0.8,
                        ""MaxTokens"": 8192
                    },
                    ""Providers"": {
                        ""claude-fast"": {
                            ""Type"": ""Anthropic"",
                            ""DisplayName"": ""Claude"",
                            ""Parameters"": {
                                ""Model"": ""claude-haiku-4-5-20251001"",
                                ""ApiKey"": ""sk-ant-test""
                            }
                        }
                    }
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act
        var config = loader.LoadFromJson(configJson);

        // Assert
        Assert.IsNotNull(config.DefaultParameters);
        Assert.AreEqual(0.9, config.DefaultParameters.Temperature);
        Assert.AreEqual(0.8, config.DefaultParameters.TopP);
        Assert.AreEqual(8192, config.DefaultParameters.MaxTokens);
    }

    [TestMethod]
    public void LoadFromJson_MissingActiveProvider_ThrowsConfigurationException()
    {
        // Arrange
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""LLM"": {
                    ""Providers"": {
                        ""claude-fast"": {
                            ""Type"": ""Anthropic"",
                            ""DisplayName"": ""Claude"",
                            ""Parameters"": {
                                ""Model"": ""claude-haiku-4-5-20251001"",
                                ""ApiKey"": ""sk-ant-test""
                            }
                        }
                    }
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => loader.LoadFromJson(configJson));
    }

    [TestMethod]
    public void LoadFromJson_InvalidActiveProvider_ThrowsConfigurationException()
    {
        // Arrange
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""LLM"": {
                    ""ActiveProvider"": ""nonexistent-provider"",
                    ""Providers"": {
                        ""claude-fast"": {
                            ""Type"": ""Anthropic"",
                            ""DisplayName"": ""Claude"",
                            ""Parameters"": {
                                ""Model"": ""claude-haiku-4-5-20251001"",
                                ""ApiKey"": ""sk-ant-test""
                            }
                        }
                    }
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => loader.LoadFromJson(configJson));
    }

    [TestMethod]
    public void LoadFromJson_NoProviders_ThrowsConfigurationException()
    {
        // Arrange
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""LLM"": {
                    ""ActiveProvider"": ""claude-fast""
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => loader.LoadFromJson(configJson));
    }

    [TestMethod]
    public void LoadFromJson_EmptyProviders_ThrowsConfigurationException()
    {
        // Arrange
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""LLM"": {
                    ""ActiveProvider"": ""claude-fast"",
                    ""Providers"": {}
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => loader.LoadFromJson(configJson));
    }

    [TestMethod]
    public void LoadFromJson_MissingLLMSection_ThrowsConfigurationException()
    {
        // Arrange
        var configJson = @"{
            ""TransparentAiAgent"": {
                ""Agent"": {
                    ""SystemPrompt"": ""test""
                }
            }
        }";

        var loader = new LLMConfigurationLoader();

        // Act & Assert
        Assert.ThrowsException<ConfigurationException>(() => loader.LoadFromJson(configJson));
    }
}
