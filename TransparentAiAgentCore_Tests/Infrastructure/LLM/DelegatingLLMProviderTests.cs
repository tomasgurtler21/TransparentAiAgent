using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.LLM;

namespace TransparentAiAgentCore_Tests.Infrastructure.LLM;

[TestClass]
public class DelegatingLLMProviderTests
{
    [TestMethod]
    public void Constructor_NullManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new DelegatingLLMProvider(null!));
    }

    [TestMethod]
    public async Task SendRequestAsync_UsesCurrentActiveProvider()
    {
        // Arrange
        var mockManager = new Mock<ILLMProviderManager>();
        var mockProvider1 = new Mock<ILLMProvider>();
        var mockProvider2 = new Mock<ILLMProvider>();

        var response1 = new LLMResponse(
            Content: "Response from Provider 1",
            ToolCalls: null,
            Thinking: null);
        var response2 = new LLMResponse(
            Content: "Response from Provider 2",
            ToolCalls: null,
            Thinking: null);

        mockProvider1.Setup(p => p.SendRequestAsync(It.IsAny<LLMRequest>(), default))
            .ReturnsAsync(response1);
        mockProvider2.Setup(p => p.SendRequestAsync(It.IsAny<LLMRequest>(), default))
            .ReturnsAsync(response2);

        // Initially return provider 1
        mockManager.Setup(m => m.GetActiveProvider()).Returns(mockProvider1.Object);

        var delegatingProvider = new DelegatingLLMProvider(mockManager.Object);
        var request = new LLMRequest(
            Messages: new List<LLMMessage>(),
            Temperature: null,
            TopP: null,
            MaxTokens: null,
            Stream: false,
            Tools: null);

        // Act - First call with provider 1 active
        var result1 = await delegatingProvider.SendRequestAsync(request);

        // Assert - Should use provider 1
        Assert.AreEqual("Response from Provider 1", result1.Content);
        mockProvider1.Verify(p => p.SendRequestAsync(request, default), Times.Once);

        // Switch to provider 2
        mockManager.Setup(m => m.GetActiveProvider()).Returns(mockProvider2.Object);

        // Act - Second call with provider 2 active
        var result2 = await delegatingProvider.SendRequestAsync(request);

        // Assert - Should use provider 2 (NOT cached provider 1)
        Assert.AreEqual("Response from Provider 2", result2.Content);
        mockProvider2.Verify(p => p.SendRequestAsync(request, default), Times.Once);

        // Verify provider 1 was only called once (not on second request)
        mockProvider1.Verify(p => p.SendRequestAsync(request, default), Times.Once);
    }

    [TestMethod]
    public async Task StreamRequestAsync_UsesCurrentActiveProvider()
    {
        // Arrange
        var mockManager = new Mock<ILLMProviderManager>();
        var mockProvider = new Mock<ILLMProvider>();

        var chunks = new List<StreamingChunk>
        {
            new StreamingChunk(ContentDelta: "Hello", IsComplete: false),
            new StreamingChunk(ContentDelta: " World", IsComplete: true)
        };

        async IAsyncEnumerable<StreamingChunk> GetChunks()
        {
            foreach (var chunk in chunks)
            {
                yield return chunk;
                await Task.Delay(1); // Simulate async streaming
            }
        }

        mockProvider.Setup(p => p.StreamRequestAsync(It.IsAny<LLMRequest>(), default))
            .Returns(GetChunks());

        mockManager.Setup(m => m.GetActiveProvider()).Returns(mockProvider.Object);

        var delegatingProvider = new DelegatingLLMProvider(mockManager.Object);
        var request = new LLMRequest(
            Messages: new List<LLMMessage>(),
            Temperature: null,
            TopP: null,
            MaxTokens: null,
            Stream: true,
            Tools: null);

        // Act
        var resultChunks = new List<StreamingChunk>();
        await foreach (var chunk in delegatingProvider.StreamRequestAsync(request))
        {
            resultChunks.Add(chunk);
        }

        // Assert
        Assert.AreEqual(2, resultChunks.Count);
        Assert.AreEqual("Hello", resultChunks[0].ContentDelta);
        Assert.AreEqual(" World", resultChunks[1].ContentDelta);
        Assert.IsFalse(resultChunks[0].IsComplete);
        Assert.IsTrue(resultChunks[1].IsComplete);

        mockProvider.Verify(p => p.StreamRequestAsync(request, default), Times.Once);
    }

    [TestMethod]
    public async Task SendRequestAsync_CallsGetActiveProviderEachTime()
    {
        // Arrange
        var mockManager = new Mock<ILLMProviderManager>();
        var mockProvider = new Mock<ILLMProvider>();

        var response = new LLMResponse(
            Content: "Response",
            ToolCalls: null,
            Thinking: null);

        mockProvider.Setup(p => p.SendRequestAsync(It.IsAny<LLMRequest>(), default))
            .ReturnsAsync(response);

        mockManager.Setup(m => m.GetActiveProvider()).Returns(mockProvider.Object);

        var delegatingProvider = new DelegatingLLMProvider(mockManager.Object);
        var request = new LLMRequest(
            Messages: new List<LLMMessage>(),
            Temperature: null,
            TopP: null,
            MaxTokens: null,
            Stream: false,
            Tools: null);

        // Act - Call 3 times
        await delegatingProvider.SendRequestAsync(request);
        await delegatingProvider.SendRequestAsync(request);
        await delegatingProvider.SendRequestAsync(request);

        // Assert - GetActiveProvider should be called 3 times (once per request)
        mockManager.Verify(m => m.GetActiveProvider(), Times.Exactly(3));
    }
}
