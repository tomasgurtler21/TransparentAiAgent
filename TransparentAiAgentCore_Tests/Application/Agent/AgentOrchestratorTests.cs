using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore_Tests.Application.Agent
{
    [TestClass]
    public class AgentOrchestratorTests
    {
        private MockLLMProvider _mockLLMProvider;
        private IConversationManager _conversationManager;
        private IMessagePipeline _messagePipeline;
        private ITransparencyService _transparencyService;
        private AppConfiguration _configuration;

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
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_NullLLMProvider_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new AgentOrchestrator(null!, _conversationManager, _messagePipeline, _transparencyService, _configuration));
        }

        [TestMethod]
        public void Constructor_NullConversationManager_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new AgentOrchestrator(_mockLLMProvider, null!, _messagePipeline, _transparencyService, _configuration));
        }

        [TestMethod]
        public void Constructor_NullMessagePipeline_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new AgentOrchestrator(_mockLLMProvider, _conversationManager, null!, _transparencyService, _configuration));
        }

        [TestMethod]
        public void Constructor_NullTransparencyService_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, null!, _configuration));
        }

        [TestMethod]
        public void Constructor_NullConfiguration_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, null!));
        }

        [TestMethod]
        public void Constructor_AddsSystemMessageToConversation()
        {
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            var messages = _conversationManager.GetAllMessages();
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("You are a test assistant.", messages[0].Content);
        }

        [TestMethod]
        public void Constructor_LogsInitializationEvent()
        {
            _transparencyService.ClearEvents();

            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("initialized")));
        }

        #endregion

        #region ProcessUserInputAsync Tests

        [TestMethod]
        public async Task ProcessUserInputAsync_NullInput_ThrowsArgumentException()
        {
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                orchestrator.ProcessUserInputAsync(null!));
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_WhitespaceInput_ThrowsArgumentException()
        {
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                orchestrator.ProcessUserInputAsync("   "));
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_AddsUserMessageToConversation()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await orchestrator.ProcessUserInputAsync("Hello");

            var messages = _conversationManager.GetAllMessages();
            Assert.IsTrue(messages.Any(m => m.Content == "Hello"));
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_CallsLLMProvider()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("AI Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await orchestrator.ProcessUserInputAsync("Test input");

            Assert.IsTrue(_mockLLMProvider.WasSendRequestCalled);
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_AddsAssistantMessageToConversation()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("AI Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await orchestrator.ProcessUserInputAsync("Hello");

            var messages = _conversationManager.GetAllMessages();
            Assert.IsTrue(messages.Any(m => m.Content == "AI Response"));
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_ReturnsAssistantMessage()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("AI Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            var result = await orchestrator.ProcessUserInputAsync("Hello");

            Assert.AreEqual("AI Response", result.Content);
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_LogsUserInputEvent()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);
            _transparencyService.ClearEvents();

            await orchestrator.ProcessUserInputAsync("Test");

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("User input")));
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_LogsLLMRequestAndResponseEvents()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);
            _transparencyService.ClearEvents();

            await orchestrator.ProcessUserInputAsync("Test");

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("request to LLM")));
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("Received response")));
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_BuildsRequestWithInContextMessagesOnly()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await orchestrator.ProcessUserInputAsync("Hello");

            var lastRequest = _mockLLMProvider.LastRequest;
            Assert.IsNotNull(lastRequest);
            // System message (1) + User message (1) = 2
            Assert.AreEqual(2, lastRequest.Messages.Count);
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_WrapsNonAgentExceptions()
        {
            _mockLLMProvider.SetNextException(new InvalidOperationException("Test error"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await Assert.ThrowsExceptionAsync<AgentException>(() =>
                orchestrator.ProcessUserInputAsync("Test"));
        }

        [TestMethod]
        public async Task ProcessUserInputAsync_PropagatesLLMException()
        {
            _mockLLMProvider.SetNextException(new LLMException("LLM Error"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await Assert.ThrowsExceptionAsync<LLMException>(() =>
                orchestrator.ProcessUserInputAsync("Test"));
        }

        #endregion

        #region ProcessUserInputStreamingAsync Tests

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_NullInput_ThrowsArgumentException()
        {
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync(null!))
                {
                    // Should not get here
                }
            });
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_YieldsChunksFromProvider()
        {
            _mockLLMProvider.SetStreamingChunks(new[]
            {
                new StreamingLLMChunk("Hello", null, false, null),
                new StreamingLLMChunk(" World", null, false, null),
                new StreamingLLMChunk("!", null, true, "stop")
            });
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            var chunks = new List<string>();
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Test"))
            {
                chunks.Add(chunk.ContentDelta);
            }

            Assert.AreEqual(3, chunks.Count);
            Assert.AreEqual("Hello", chunks[0]);
            Assert.AreEqual(" World", chunks[1]);
            Assert.AreEqual("!", chunks[2]);
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_AddsCompleteMessageToConversation()
        {
            _mockLLMProvider.SetStreamingChunks(new[]
            {
                new StreamingLLMChunk("Hello", null, false, null),
                new StreamingLLMChunk(" World", null, true, "stop")
            });
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Test"))
            {
                // Consume chunks
            }

            var messages = _conversationManager.GetAllMessages();
            Assert.IsTrue(messages.Any(m => m.Content == "Hello World"));
        }

        #endregion

        #region StartNewConversation Tests

        [TestMethod]
        public void StartNewConversation_ClearsConversation()
        {
            _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);

            // Add some messages
            orchestrator.ProcessUserInputAsync("Test1").Wait();

            orchestrator.StartNewConversation();

            // Should only have system message
            var messages = _conversationManager.GetAllMessages();
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("You are a test assistant.", messages[0].Content);
        }

        [TestMethod]
        public void StartNewConversation_LogsRestartEvent()
        {
            var orchestrator = new AgentOrchestrator(_mockLLMProvider, _conversationManager, _messagePipeline, _transparencyService, _configuration);
            _transparencyService.ClearEvents();

            orchestrator.StartNewConversation();

            var events = _transparencyService.GetEvents();
            Assert.IsTrue(events.Any(e => e.AdditionalInfo != null && e.AdditionalInfo.Contains("new conversation")));
        }

        #endregion

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
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                LastRequest = request;

                if (_streamingChunks != null)
                {
                    foreach (var chunk in _streamingChunks)
                    {
                        await Task.Delay(1, cancellationToken); // Simulate async
                        yield return chunk;
                    }
                }
            }
        }

        #endregion
    }
}
