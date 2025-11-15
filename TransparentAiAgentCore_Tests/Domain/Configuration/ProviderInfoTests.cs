using TransparentAiAgentCore.Domain.Configuration;

namespace TransparentAiAgentCore_Tests.Domain.Configuration;

[TestClass]
public class ProviderInfoTests
{
    [TestMethod]
    public void ProviderInfo_NullConfigName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ProviderInfo(
                configName: null!,
                displayName: "Test",
                providerType: "Anthropic",
                modelName: "claude-haiku",
                isActive: false
            ));
    }

    [TestMethod]
    public void ProviderInfo_NullDisplayName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ProviderInfo(
                configName: "test",
                displayName: null!,
                providerType: "Anthropic",
                modelName: "claude-haiku",
                isActive: false
            ));
    }

    [TestMethod]
    public void ProviderInfo_NullProviderType_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ProviderInfo(
                configName: "test",
                displayName: "Test",
                providerType: null!,
                modelName: "claude-haiku",
                isActive: false
            ));
    }

    [TestMethod]
    public void ProviderInfo_NullModelName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ProviderInfo(
                configName: "test",
                displayName: "Test",
                providerType: "Anthropic",
                modelName: null!,
                isActive: false
            ));
    }

    [TestMethod]
    public void ProviderInfo_ValidParameters_SetsAllProperties()
    {
        // Act
        var info = new ProviderInfo(
            configName: "claude-fast",
            displayName: "Claude Haiku (Fast)",
            providerType: "Anthropic",
            modelName: "claude-haiku-4-5-20251001",
            isActive: true
        );

        // Assert
        Assert.AreEqual("claude-fast", info.ConfigName);
        Assert.AreEqual("Claude Haiku (Fast)", info.DisplayName);
        Assert.AreEqual("Anthropic", info.ProviderType);
        Assert.AreEqual("claude-haiku-4-5-20251001", info.ModelName);
        Assert.IsTrue(info.IsActive);
    }

    [TestMethod]
    public void ProviderInfo_IsActiveFalse_SetsCorrectly()
    {
        // Act
        var info = new ProviderInfo(
            configName: "azure-gpt4",
            displayName: "Azure GPT-4",
            providerType: "AzureOpenAI",
            modelName: "gpt-4",
            isActive: false
        );

        // Assert
        Assert.IsFalse(info.IsActive);
    }
}
