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
    public class AgentOrchestratorApplicationMessageTests
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
        public async Task ProcessApplicationMessageAsync_ScenarioUserMessage_AddsToConversation()
        {
            // Arrange
            _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
            var msg = new ScenarioUserMessage("Test", "Annotation");

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessApplicationMessageAsync(msg));

            // Assert
            var messages = _conversationManager.GetAllMessages();
            Assert.IsTrue(messages.Any(m => m is ScenarioUserMessage));
        }

        [TestMethod]
        public async Task ProcessApplicationMessageAsync_ScenarioUserMessage_CallsLLM()
        {
            // Arrange
            _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
            var msg = new ScenarioUserMessage("Test");

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessApplicationMessageAsync(msg));

            // Assert
            Assert.IsTrue(_mockLLMProvider.WasSendRequestCalled);
        }

        [TestMethod]
        public async Task ProcessApplicationMessageAsync_ScenarioAssistantMessage_DoesNotCallLLM()
        {
            // Arrange
            var msg = new ScenarioAssistantMessage("Response", "Teaching");

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessApplicationMessageAsync(msg));

            // Assert
            Assert.IsFalse(_mockLLMProvider.WasSendRequestCalled);
        }

        [TestMethod]
        public async Task ProcessApplicationMessageAsync_ScenarioAssistantMessage_AddsToConversation()
        {
            // Arrange
            var msg = new ScenarioAssistantMessage("Response", "Teaching");

            // Act
            await ConsumeStreamAsync(_orchestrator.ProcessApplicationMessageAsync(msg));

            // Assert
            var messages = _conversationManager.GetAllMessages();
            Assert.IsTrue(messages.Any(m => m is ScenarioAssistantMessage));
        }

        [TestMethod]
        public async Task ProcessApplicationMessageAsync_ScenarioUserMessage_YieldsResponseChunks()
        {
            // Arrange
            _mockLLMProvider.SetStreamingChunks(new StreamingLLMChunk[]
            {
                new StreamingLLMChunk("Hello"),
                new StreamingLLMChunk(" "),
                new StreamingLLMChunk("World", isComplete: true)
            });
            var msg = new ScenarioUserMessage("Test");

            // Act
            var chunks = new List<StreamingResponseChunk>();
            await foreach (var chunk in _orchestrator.ProcessApplicationMessageAsync(msg))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.IsTrue(chunks.Count > 0);
            Assert.IsTrue(chunks.Any(c => c.ContentDelta != null));
        }

        [TestMethod]
        public async Task ProcessApplicationMessageAsync_ScenarioAssistantMessage_YieldsCompletionChunk()
        {
            // Arrange
            var msg = new ScenarioAssistantMessage("Response");

            // Act
            var chunks = new List<StreamingResponseChunk>();
            await foreach (var chunk in _orchestrator.ProcessApplicationMessageAsync(msg))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.IsTrue(chunks.Count > 0);
            Assert.IsTrue(chunks.Any(c => c.IsComplete));
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
