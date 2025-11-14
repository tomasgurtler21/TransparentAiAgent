using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.Transparency;
using System.Runtime.CompilerServices;
using TransparentAiAgentCore.Domain.Tools;

namespace TransparentAiAgentCore_Tests.Application.Agent
{
    [TestClass]
    public class AgentOrchestratorToolMessageTests
    {
        private MockLLMProvider _mockLLMProvider;
        private IConversationManager _conversationManager;
        private IMessagePipeline _messagePipeline;
        private ITransparencyService _transparencyService;
        private AppConfiguration _configuration;
        private AgentOrchestrator _orchestrator;

        [TestInitialize]
        public void Setup()
        {
            _mockLLMProvider = new MockLLMProvider();
            _transparencyService = new TransparencyService();
            _conversationManager = new ConversationManager(10, _transparencyService);
            _messagePipeline = new MessagePipeline();

            _configuration = new AppConfiguration
            {
                Agent = new AgentConfiguration
                {
                    SystemPrompt = "You are a test assistant.",
                    ContextWindowSize = 10
                },
                LLM = new LLMConfiguration
                {
                    Provider = "MockProvider",
                    Temperature = 0.7,
                    TopP = 1.0,
                    MaxTokens = 1000
                }
            };

            _orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration);
        }

        [TestMethod]
        public async Task ProcessToolMessageAsync_ToolResultMessage_AddsToConversation()
        {
            // Arrange
            _mockLLMProvider.SetNextResponse(new LLMResponse("Processed"));
            var msg = new ToolResultMessage("tool_call_123", "get_weather", "{\"temp\":20}", false);

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(msg));

            // Assert
            var messages = _conversationManager.GetAllMessages();
            Assert.IsTrue(messages.Any(m => m is ToolResultMessage));
        }

        [TestMethod]
        public async Task ProcessToolMessageAsync_ToolResultMessage_CallsLLM()
        {
            // Arrange
            _mockLLMProvider.SetNextResponse(new LLMResponse("Processed"));
            var msg = new ToolResultMessage("tool_call_123", "get_weather", "{\"temp\":20}", false);

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(msg));

            // Assert
            Assert.IsTrue(_mockLLMProvider.WasSendRequestCalled);
        }

        [TestMethod]
        public async Task ProcessToolMessageAsync_ToolErrorMessage_AddsToConversation()
        {
            // Arrange
            _mockLLMProvider.SetNextResponse(new LLMResponse("Handled error"));
            var msg = new ToolErrorMessage("tool_call_456", "get_weather", "API error");

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(msg));

            // Assert
            var messages = _conversationManager.GetAllMessages();
            Assert.IsTrue(messages.Any(m => m is ToolErrorMessage));
        }

        [TestMethod]
        public async Task ProcessToolMessageAsync_ToolErrorMessage_CallsLLM()
        {
            // Arrange
            _mockLLMProvider.SetNextResponse(new LLMResponse("Handled error"));
            var msg = new ToolErrorMessage("tool_call_456", "get_weather", "API error");

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(msg));

            // Assert
            Assert.IsTrue(_mockLLMProvider.WasSendRequestCalled);
        }

        [TestMethod]
        public async Task ProcessToolMessageAsync_ToolResultMessage_YieldsResponseChunks()
        {
            // Arrange
            _mockLLMProvider.SetStreamingChunks(new StreamingLLMChunk[]
            {
                new StreamingLLMChunk("Weather"),
                new StreamingLLMChunk(" processed", isComplete: true)
            });
            var msg = new ToolResultMessage("tool_call_123", "get_weather", "{\"temp\":20}", false);

            // Act
            var chunks = new List<StreamingResponseChunk>();
            await foreach (var chunk in _orchestrator.ProcessToolMessageAsync(msg))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.IsTrue(chunks.Count > 0);
            Assert.IsTrue(chunks.Any(c => c.ContentDelta != null));
        }

        [TestMethod]
        public async Task ProcessToolMessageAsync_ToolErrorMessage_YieldsResponseChunks()
        {
            // Arrange
            _mockLLMProvider.SetStreamingChunks(new StreamingLLMChunk[]
            {
                new StreamingLLMChunk("Error handled", isComplete: true)
            });
            var msg = new ToolErrorMessage("tool_call_456", "get_weather", "API error");

            // Act
            var chunks = new List<StreamingResponseChunk>();
            await foreach (var chunk in _orchestrator.ProcessToolMessageAsync(msg))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.IsTrue(chunks.Count > 0);
        }

        [TestMethod]
        public async Task ProcessToolMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                async () => await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(null!)));
        }

        /// <summary>
        /// Helper method to consume async stream completely
        /// </summary>
        private async Task ConsumeStreamAsync(IAsyncEnumerable<StreamingResponseChunk> stream)
        {
            await foreach (var chunk in stream)
            {
                // Just consume the stream
            }
        }

        #region Helper Classes

        /// <summary>
        /// Mock LLM provider for testing
        /// </summary>
        private class MockLLMProvider : ILLMProvider
        {
            private LLMResponse? _nextResponse;
            private Exception? _nextException;
            private StreamingLLMChunk[]? _streamingChunks;

            public string ProviderName => "MockProvider";
            public bool WasSendRequestCalled { get; private set; }
            public LLMRequest? LastRequest { get; private set; }

            public void SetNextResponse(LLMResponse response)
            {
                _nextResponse = response;
                _nextException = null;
            }

            public void SetNextException(Exception exception)
            {
                _nextException = exception;
                _nextResponse = null;
            }

            public void SetStreamingChunks(StreamingLLMChunk[] chunks)
            {
                _streamingChunks = chunks;
            }

            public Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken = default)
            {
                WasSendRequestCalled = true;
                LastRequest = request;

                if (_nextException != null)
                    throw _nextException;

                return Task.FromResult(_nextResponse ?? new LLMResponse("Default response"));
            }

            public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
                LLMRequest request,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                WasSendRequestCalled = true; // Track streaming calls as LLM calls
                LastRequest = request;

                if (_streamingChunks != null)
                {
                    foreach (var chunk in _streamingChunks)
                    {
                        await Task.Delay(1, cancellationToken); // Simulate async
                        yield return chunk;
                    }
                }
                else
                {
                    // If no streaming chunks set, yield a simple completion chunk
                    yield return new StreamingLLMChunk("", isComplete: true);
                }
            }
        }

        #endregion
    }
}
