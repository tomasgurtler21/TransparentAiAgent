using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Configuration;

namespace TransparentAiAgentCore_Tests.Infrastructure.Configuration;

[TestClass]
public class ConfigurationService_OAuthTests
{
    private ConfigurationService _service = null!;
    private string _testConfigPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new ConfigurationService();
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
    }

    [TestMethod]
    public void LoadConfiguration_WithDefaultAzureCredentialString_ShouldDeserializeCorrectly()
    {
        // Arrange - Create config file with string enum value
        var jsonContent = @"{
  ""TransparentAiAgent"": {
    ""Agent"": {
      ""SystemPrompt"": ""Test prompt"",
      ""ContextWindowSize"": 20
    },
    ""LLM"": {
      ""Provider"": ""AzureOpenAI"",
      ""AzureOpenAI"": {
        ""AuthenticationMode"": ""DefaultAzureCredential"",
        ""Endpoint"": ""https://test.openai.azure.com/"",
        ""DeploymentName"": ""gpt-4"",
        ""ApiVersion"": ""2024-02-15-preview""
      }
    }
  }
}";
        File.WriteAllText(_testConfigPath, jsonContent);

        // Act
        var config = _service.LoadConfiguration(_testConfigPath);

        // Assert
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.LLM.AzureOpenAI);
        Assert.AreEqual(AuthenticationMode.DefaultAzureCredential, config.LLM.AzureOpenAI.AuthenticationMode,
            "AuthenticationMode should be deserialized as DefaultAzureCredential enum value");
    }

    [TestMethod]
    public void LoadConfiguration_WithApiKeyString_ShouldDeserializeCorrectly()
    {
        // Arrange - Create config file with string enum value
        var jsonContent = @"{
  ""TransparentAiAgent"": {
    ""LLM"": {
      ""Provider"": ""AzureOpenAI"",
      ""AzureOpenAI"": {
        ""AuthenticationMode"": ""ApiKey"",
        ""Endpoint"": ""https://test.openai.azure.com/"",
        ""ApiKey"": ""test-key"",
        ""DeploymentName"": ""gpt-4"",
        ""ApiVersion"": ""2024-02-15-preview""
      }
    }
  }
}";
        File.WriteAllText(_testConfigPath, jsonContent);

        // Act
        var config = _service.LoadConfiguration(_testConfigPath);

        // Assert
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.LLM.AzureOpenAI);
        Assert.AreEqual(AuthenticationMode.ApiKey, config.LLM.AzureOpenAI.AuthenticationMode,
            "AuthenticationMode should be deserialized as ApiKey enum value");
    }

    [TestMethod]
    public void Validate_DefaultAzureCredentialWithoutApiKey_ShouldPass()
    {
        // Arrange - Create OAuth config WITHOUT ApiKey
        var jsonContent = @"{
  ""TransparentAiAgent"": {
    ""LLM"": {
      ""ActiveProvider"": ""azure-gpt4"",
      ""Providers"": {
        ""azure-gpt4"": {
          ""Type"": ""AzureOpenAI"",
          ""DisplayName"": ""Azure GPT-4"",
          ""Parameters"": {
            ""AuthenticationMode"": ""DefaultAzureCredential"",
            ""Endpoint"": ""https://test.openai.azure.com/"",
            ""DeploymentName"": ""gpt-4"",
            ""ApiVersion"": ""2024-02-15-preview"",
            ""IsReasoningModel"": false
          }
        }
      },
      ""Provider"": ""AzureOpenAI"",
      ""AzureOpenAI"": {
        ""AuthenticationMode"": ""DefaultAzureCredential"",
        ""Endpoint"": ""https://test.openai.azure.com/"",
        ""DeploymentName"": ""gpt-4"",
        ""ApiVersion"": ""2024-02-15-preview""
      }
    }
  }
}";
        File.WriteAllText(_testConfigPath, jsonContent);

        // Act
        var config = _service.LoadConfiguration(_testConfigPath);

        // Assert - Should NOT throw because OAuth doesn't require ApiKey
        config.LLM.Validate();
        Assert.AreEqual(AuthenticationMode.DefaultAzureCredential, config.LLM.AzureOpenAI!.AuthenticationMode);
    }

    [TestMethod]
    public void Validate_DefaultAzureCredentialWithTenantId_ShouldPass()
    {
        // Arrange - Create OAuth config with TenantId
        var jsonContent = @"{
  ""TransparentAiAgent"": {
    ""LLM"": {
      ""ActiveProvider"": ""azure-gpt4"",
      ""Providers"": {
        ""azure-gpt4"": {
          ""Type"": ""AzureOpenAI"",
          ""DisplayName"": ""Azure GPT-4"",
          ""Parameters"": {
            ""AuthenticationMode"": ""DefaultAzureCredential"",
            ""Endpoint"": ""https://test.openai.azure.com/"",
            ""DeploymentName"": ""gpt-4"",
            ""ApiVersion"": ""2024-02-15-preview"",
            ""TenantId"": ""12345678-1234-1234-1234-123456789012"",
            ""IsReasoningModel"": false
          }
        }
      },
      ""Provider"": ""AzureOpenAI"",
      ""AzureOpenAI"": {
        ""AuthenticationMode"": ""DefaultAzureCredential"",
        ""Endpoint"": ""https://test.openai.azure.com/"",
        ""DeploymentName"": ""gpt-4"",
        ""ApiVersion"": ""2024-02-15-preview"",
        ""TenantId"": ""12345678-1234-1234-1234-123456789012""
      }
    }
  }
}";
        File.WriteAllText(_testConfigPath, jsonContent);

        // Act
        var config = _service.LoadConfiguration(_testConfigPath);

        // Assert
        config.LLM.Validate();
        Assert.AreEqual(AuthenticationMode.DefaultAzureCredential, config.LLM.AzureOpenAI!.AuthenticationMode);
        Assert.AreEqual("12345678-1234-1234-1234-123456789012", config.LLM.AzureOpenAI.TenantId);
    }
}
