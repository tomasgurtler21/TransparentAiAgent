using TransparentAiAgentCore.Domain.Transparency.EventData;

namespace TransparentAiAgentCore_Tests.Domain.Transparency.EventData;

[TestClass]
public class RawLLMRequestDataTests
{
    [TestMethod]
    public void Constructor_ValidInputs_InitializesAllProperties()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        var provider = "AzureOpenAI";
        var requestJson = "{\"test\": \"data\"}";
        var messageCount = 5;
        var beforeTimestamp = DateTime.UtcNow;

        // Act
        var data = new RawLLMRequestData(correlationId, provider, requestJson, messageCount);
        var afterTimestamp = DateTime.UtcNow;

        // Assert
        Assert.AreEqual(correlationId, data.CorrelationId);
        Assert.AreEqual(provider, data.Provider);
        Assert.AreEqual(requestJson, data.RequestJson);
        Assert.AreEqual(messageCount, data.MessageCount);
        Assert.IsTrue(data.SentAt >= beforeTimestamp && data.SentAt <= afterTimestamp);
    }

    [TestMethod]
    public void Constructor_NullCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMRequestData(null!, "AzureOpenAI", "{}", 1));
    }

    [TestMethod]
    public void Constructor_EmptyCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMRequestData("", "AzureOpenAI", "{}", 1));
    }

    [TestMethod]
    public void Constructor_WhitespaceCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMRequestData("   ", "AzureOpenAI", "{}", 1));
    }

    [TestMethod]
    public void Constructor_NullProvider_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMRequestData("test-id", null!, "{}", 1));
    }

    [TestMethod]
    public void Constructor_NullRequestJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMRequestData("test-id", "AzureOpenAI", null!, 1));
    }

    [TestMethod]
    public void Constructor_NegativeMessageCount_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMRequestData("test-id", "AzureOpenAI", "{}", -1));
    }

    [TestMethod]
    public void Constructor_ZeroMessageCount_Allowed()
    {
        // Arrange & Act
        var data = new RawLLMRequestData("test-id", "AzureOpenAI", "{}", 0);

        // Assert
        Assert.AreEqual(0, data.MessageCount);
    }
}
