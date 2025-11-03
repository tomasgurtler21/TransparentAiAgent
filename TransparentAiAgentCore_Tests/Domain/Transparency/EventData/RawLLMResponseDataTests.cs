using TransparentAiAgentCore.Domain.Transparency.EventData;

namespace TransparentAiAgentCore_Tests.Domain.Transparency.EventData;

[TestClass]
public class RawLLMResponseDataTests
{
    [TestMethod]
    public void Constructor_ValidInputs_InitializesAllProperties()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        var provider = "AzureOpenAI";
        var responseJson = "{\"response\": \"data\"}";
        var statusCode = 200;
        var latency = TimeSpan.FromMilliseconds(500);
        var beforeTimestamp = DateTime.UtcNow;

        // Act
        var data = new RawLLMResponseData(correlationId, provider, responseJson, statusCode, latency);
        var afterTimestamp = DateTime.UtcNow;

        // Assert
        Assert.AreEqual(correlationId, data.CorrelationId);
        Assert.AreEqual(provider, data.Provider);
        Assert.AreEqual(responseJson, data.ResponseJson);
        Assert.AreEqual(statusCode, data.StatusCode);
        Assert.AreEqual(latency, data.Latency);
        Assert.IsTrue(data.ReceivedAt >= beforeTimestamp && data.ReceivedAt <= afterTimestamp);
    }

    [TestMethod]
    public void Constructor_NullCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMResponseData(null!, "AzureOpenAI", "{}", 200, TimeSpan.Zero));
    }

    [TestMethod]
    public void Constructor_EmptyCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMResponseData("", "AzureOpenAI", "{}", 200, TimeSpan.Zero));
    }

    [TestMethod]
    public void Constructor_WhitespaceCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMResponseData("   ", "AzureOpenAI", "{}", 200, TimeSpan.Zero));
    }

    [TestMethod]
    public void Constructor_NullProvider_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMResponseData("test-id", null!, "{}", 200, TimeSpan.Zero));
    }

    [TestMethod]
    public void Constructor_NullResponseJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMResponseData("test-id", "AzureOpenAI", null!, 200, TimeSpan.Zero));
    }

    [TestMethod]
    public void Constructor_NullStatusCode_Allowed()
    {
        // Arrange & Act
        var data = new RawLLMResponseData("test-id", "AzureOpenAI", "{}", null, TimeSpan.Zero);

        // Assert
        Assert.IsNull(data.StatusCode);
    }

    [TestMethod]
    public void Constructor_NegativeLatency_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new RawLLMResponseData("test-id", "AzureOpenAI", "{}", 200, TimeSpan.FromMilliseconds(-100)));
    }

    [TestMethod]
    public void Constructor_ZeroLatency_Allowed()
    {
        // Arrange & Act
        var data = new RawLLMResponseData("test-id", "AzureOpenAI", "{}", 200, TimeSpan.Zero);

        // Assert
        Assert.AreEqual(TimeSpan.Zero, data.Latency);
    }
}
