using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Infrastructure.LLM;

namespace TransparentAiAgentCore_Tests.Infrastructure.LLM;

[TestClass]
public class LLMProviderManagerTests
{
    private Mock<ILLMProviderFactory> _mockFactory = null!;
    private Mock<ILLMProvider> _mockProvider1 = null!;
    private Mock<ILLMProvider> _mockProvider2 = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockFactory = new Mock<ILLMProviderFactory>();
        _mockProvider1 = new Mock<ILLMProvider>();
        _mockProvider2 = new Mock<ILLMProvider>();
    }

    #region Test 1: Get active provider (lazy-load and cache)

    [TestMethod]
    public void GetActiveProvider_FirstCall_CreatesAndCachesProvider()
    {
        // Arrange
        var config = CreateTestConfiguration(); // Helper method

        _mockFactory.Setup(f => f.CreateProvider(It.IsAny<string>(), It.IsAny<ProviderConfig>()))
            .Returns(_mockProvider1.Object);

        var manager = new LLMProviderManager(config, _mockFactory.Object);

        // Act
        var provider1 = manager.GetActiveProvider();
        var provider2 = manager.GetActiveProvider();

        // Assert
        Assert.AreSame(provider1, provider2); // Same instance (cached)
        _mockFactory.Verify(f => f.CreateProvider(It.IsAny<string>(), It.IsAny<ProviderConfig>()),
            Times.Once); // Only created once
    }

    [TestMethod]
    public void GetActiveProvider_NoActiveProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new LLMConfiguration
        {
            ActiveProvider = null,
            Providers = new Dictionary<string, ProviderConfig>()
        };

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
            new LLMProviderManager(config, _mockFactory.Object));
    }

    #endregion

    #region Test 2: Set active provider

    [TestMethod]
    public async Task SetActiveProviderAsync_ValidProvider_SwitchesProvider()
    {
        // Arrange
        var config = CreateTestConfiguration(); // Has both providers

        _mockFactory.Setup(f => f.CreateProvider("claude-fast", It.IsAny<ProviderConfig>()))
            .Returns(_mockProvider1.Object);
        _mockFactory.Setup(f => f.CreateProvider("azure-gpt4", It.IsAny<ProviderConfig>()))
            .Returns(_mockProvider2.Object);

        var manager = new LLMProviderManager(config, _mockFactory.Object);

        // Act
        var providerBefore = manager.GetActiveProvider(); // Gets claude-fast
        await manager.SetActiveProviderAsync("azure-gpt4");
        var providerAfter = manager.GetActiveProvider();

        // Assert
        Assert.AreSame(_mockProvider1.Object, providerBefore);
        Assert.AreSame(_mockProvider2.Object, providerAfter);
    }

    [TestMethod]
    public async Task SetActiveProviderAsync_InvalidProvider_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var manager = new LLMProviderManager(config, _mockFactory.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            manager.SetActiveProviderAsync("nonexistent-provider"));
    }

    #endregion

    #region Test 3: Get available providers

    [TestMethod]
    public void GetAvailableProviders_MultipleProviders_ReturnsAllProviderInfo()
    {
        // Arrange
        var config = CreateTestConfiguration(); // Has claude-fast and azure-gpt4
        var manager = new LLMProviderManager(config, _mockFactory.Object);

        // Act
        var providers = manager.GetAvailableProviders();

        // Assert
        Assert.AreEqual(2, providers.Count);
        Assert.IsTrue(providers.Any(p => p.ConfigName == "claude-fast"));
        Assert.IsTrue(providers.Any(p => p.ConfigName == "azure-gpt4"));
        Assert.IsTrue(providers.Single(p => p.ConfigName == "claude-fast").IsActive);
        Assert.IsFalse(providers.Single(p => p.ConfigName == "azure-gpt4").IsActive);
    }

    #endregion

    #region Test 4: Get current provider info

    [TestMethod]
    public void GetCurrentProviderInfo_ReturnsActiveProviderInfo()
    {
        // Arrange
        var config = CreateTestConfiguration(); // ActiveProvider = claude-fast
        var manager = new LLMProviderManager(config, _mockFactory.Object);

        // Act
        var info = manager.GetCurrentProviderInfo();

        // Assert
        Assert.AreEqual("claude-fast", info.ConfigName);
        Assert.IsTrue(info.IsActive);
    }

    #endregion

    #region Helper Methods

    private LLMConfiguration CreateTestConfiguration()
    {
        return new LLMConfiguration
        {
            ActiveProvider = "claude-fast",
            DefaultParameters = new ProviderParameters(0.7, 1.0, 4096),
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["claude-fast"] = new ProviderConfig(
                    type: "Anthropic",
                    displayName: "Claude Haiku Fast",
                    parameters: new Dictionary<string, object>
                    {
                        ["Model"] = "claude-haiku-4-5-20251001",
                        ["ApiKey"] = "sk-ant-test"
                    }
                ),
                ["azure-gpt4"] = new ProviderConfig(
                    type: "AzureOpenAI",
                    displayName: "Azure GPT-4",
                    parameters: new Dictionary<string, object>
                    {
                        ["Endpoint"] = "https://test.openai.azure.com/",
                        ["DeploymentName"] = "gpt-4",
                        ["ApiKey"] = "test-key"
                    }
                )
            }
        };
    }

    #endregion
}
