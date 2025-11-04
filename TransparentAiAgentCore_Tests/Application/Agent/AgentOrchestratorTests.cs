using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.Tools;
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

            var chunks = new List<string?>();
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Test"))
            {
                chunks.Add(chunk.ContentDelta);
            }

            // Now yields 4 chunks: 3 content chunks + 1 completion chunk
            Assert.AreEqual(4, chunks.Count);
            Assert.AreEqual("Hello", chunks[0]);
            Assert.AreEqual(" World", chunks[1]);
            Assert.AreEqual("!", chunks[2]);
            Assert.IsNull(chunks[3]); // Final completion chunk has null content
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

        #region ProcessUserInputStreamingAsync with Tool Calls Tests

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_WithToolCalls_ExecutesToolsAfterStreaming()
        {
            // Arrange
            var mockToolManager = new MockToolManager();
            mockToolManager.SetNextToolResult(ToolExecutionResult.Success("Tool executed successfully", TimeSpan.Zero));

            var orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration,
                mockToolManager);

            // Setup streaming chunks: Round 1 with tool call, Round 2 without tools
            var callCount = 0;
            _mockLLMProvider.SetStreamingChunksCallback(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // First call: LLM wants to use a tool
                    return new[]
                    {
                        new StreamingLLMChunk("I will use a tool", toolCallDelta: null, isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "test_tool", "{\"arg\":\"value\"}"), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "ToolCalls")
                    };
                }
                else
                {
                    // Second call: LLM responds with tool result, no more tools
                    return new[]
                    {
                        new StreamingLLMChunk("Tool result received", toolCallDelta: null, isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "Stop")
                    };
                }
            });

            // Act
            var streamedChunks = new List<StreamingResponseChunk>();
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Use a tool"))
            {
                streamedChunks.Add(chunk);
            }

            // Assert
            Assert.IsTrue(mockToolManager.WasToolExecuted, "Tool should have been executed");
            Assert.AreEqual("test_tool", mockToolManager.LastToolCall?.Name, "Correct tool should be called");

            // Verify streaming status chunks
            Assert.IsTrue(streamedChunks.Any(c => c.Status == StreamingStatus.ExecutingTools),
                "Should yield ExecutingTools status chunk");
            Assert.IsTrue(streamedChunks.Any(c => c.Status == StreamingStatus.Completed),
                "Should yield Completed status chunk");
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_WithToolCalls_AccumulatesToolCallsCorrectly()
        {
            // Arrange
            var mockToolManager = new MockToolManager();
            mockToolManager.SetNextToolResult(ToolExecutionResult.Success("Success", TimeSpan.Zero));

            var orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration,
                mockToolManager);

            // Setup streaming chunks simulating Azure OpenAI behavior:
            // ID in first chunk, then just argument deltas
            var callCount = 0;
            _mockLLMProvider.SetStreamingChunksCallback(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new[]
                    {
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "test_tool", ""), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "", "{\"ar"), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "", "g\":\""), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "", "value\"}"), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "ToolCalls")
                    };
                }
                else
                {
                    return new[]
                    {
                        new StreamingLLMChunk("Done", toolCallDelta: null, isComplete: true, finishReason: "Stop")
                    };
                }
            });

            // Act
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Test"))
            {
                // Consume chunks
            }

            // Assert
            Assert.IsTrue(mockToolManager.WasToolExecuted, "Tool should be executed");
            Assert.AreEqual("{\"arg\":\"value\"}", mockToolManager.LastToolCall?.Arguments,
                "Tool arguments should be accumulated correctly from multiple chunks");
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_WithMultipleToolCalls_ExecutesAllTools()
        {
            // Arrange
            var mockToolManager = new MockToolManager();
            mockToolManager.SetNextToolResult(ToolExecutionResult.Success("Success", TimeSpan.Zero));

            var orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration,
                mockToolManager);

            // Setup streaming chunks with TWO tool calls
            var callCount = 0;
            _mockLLMProvider.SetStreamingChunksCallback(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new[]
                    {
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "tool_one", "{\"arg1\":\"val1\"}"), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_2", "tool_two", "{\"arg2\":\"val2\"}"), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "ToolCalls")
                    };
                }
                else
                {
                    return new[]
                    {
                        new StreamingLLMChunk("Done", toolCallDelta: null, isComplete: true, finishReason: "Stop")
                    };
                }
            });

            // Act
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Test"))
            {
                // Consume chunks
            }

            // Assert
            Assert.AreEqual(2, mockToolManager.ExecutedToolCount, "Both tools should be executed");
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_RecursiveToolLoop_HandlesMultipleRounds()
        {
            // Arrange
            var mockToolManager = new MockToolManager();
            mockToolManager.SetNextToolResult(ToolExecutionResult.Success("Result 1", TimeSpan.Zero));

            var orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration,
                mockToolManager);

            // Round 1: LLM calls tool
            var chunks1 = new[]
            {
                new StreamingLLMChunk("First", toolCallDelta: null, isComplete: false),
                new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "tool1", "{}"), isComplete: false),
                new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "ToolCalls")
            };

            // Round 2: LLM calls another tool
            var chunks2 = new[]
            {
                new StreamingLLMChunk("Second", toolCallDelta: null, isComplete: false),
                new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_2", "tool2", "{}"), isComplete: false),
                new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "ToolCalls")
            };

            // Round 3: No more tools, final response
            var chunks3 = new[]
            {
                new StreamingLLMChunk("Final response", toolCallDelta: null, isComplete: false),
                new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "Stop")
            };

            // Queue the chunks in order
            var callCount = 0;
            _mockLLMProvider.SetStreamingChunksCallback(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => chunks1,
                    2 => chunks2,
                    _ => chunks3
                };
            });

            // Act
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Start tool loop"))
            {
                // Consume chunks
            }

            // Assert
            Assert.AreEqual(2, mockToolManager.ExecutedToolCount,
                "Should execute tools from two rounds (third round has no tools)");
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_MaxDepthReached_ReturnsErrorChunk()
        {
            // Arrange - This will cause infinite tool loop
            var mockToolManager = new MockToolManager();
            mockToolManager.SetNextToolResult(ToolExecutionResult.Success("Success", TimeSpan.Zero));

            var orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration,
                mockToolManager);

            // Always return tool calls to trigger infinite loop
            var chunksWithTools = new[]
            {
                new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "infinite_tool", "{}"), isComplete: false),
                new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "ToolCalls")
            };
            _mockLLMProvider.SetStreamingChunks(chunksWithTools);

            // Act
            var streamedChunks = new List<StreamingResponseChunk>();
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Infinite loop"))
            {
                streamedChunks.Add(chunk);
                // Safety: break after reasonable number of chunks
                if (streamedChunks.Count > 100)
                    break;
            }

            // Assert
            Assert.IsTrue(streamedChunks.Any(c => c.Status == StreamingStatus.Error),
                "Should yield error status chunk when max depth reached");
            Assert.IsTrue(streamedChunks.Any(c => c.ContentDelta != null && c.ContentDelta.Contains("maximum number of tool calls")),
                "Error message should mention maximum depth");
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_NoToolCalls_CompletesNormally()
        {
            // Arrange
            var orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration);

            var chunks = new[]
            {
                new StreamingLLMChunk("Hello", toolCallDelta: null, isComplete: false),
                new StreamingLLMChunk(" World", toolCallDelta: null, isComplete: false),
                new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "Stop")
            };
            _mockLLMProvider.SetStreamingChunks(chunks);

            // Act
            var streamedChunks = new List<StreamingResponseChunk>();
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Hello"))
            {
                streamedChunks.Add(chunk);
            }

            // Assert
            Assert.IsTrue(streamedChunks.Any(c => c.ContentDelta == "Hello"), "Should stream first chunk");
            Assert.IsTrue(streamedChunks.Any(c => c.ContentDelta == " World"), "Should stream second chunk");
            Assert.IsTrue(streamedChunks.Any(c => c.Status == StreamingStatus.Completed), "Should complete normally");
            Assert.IsFalse(streamedChunks.Any(c => c.Status == StreamingStatus.ExecutingTools),
                "Should not have ExecutingTools status without tools");
        }

        [TestMethod]
        public async Task ProcessUserInputStreamingAsync_ToolExecutionError_AddsErrorResultMessage()
        {
            // Arrange
            var mockToolManager = new MockToolManager();
            mockToolManager.SetNextToolException(new Exception("Tool execution failed"));

            var orchestrator = new AgentOrchestrator(
                _mockLLMProvider,
                _conversationManager,
                _messagePipeline,
                _transparencyService,
                _configuration,
                mockToolManager);

            var callCount = 0;
            _mockLLMProvider.SetStreamingChunksCallback(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new[]
                    {
                        new StreamingLLMChunk("", toolCallDelta: new LLMToolCall("call_1", "failing_tool", "{}"), isComplete: false),
                        new StreamingLLMChunk("", toolCallDelta: null, isComplete: true, finishReason: "ToolCalls")
                    };
                }
                else
                {
                    return new[]
                    {
                        new StreamingLLMChunk("Handled error", toolCallDelta: null, isComplete: true, finishReason: "Stop")
                    };
                }
            });

            // Act
            await foreach (var chunk in orchestrator.ProcessUserInputStreamingAsync("Test"))
            {
                // Consume chunks - should not throw
            }

            // Assert
            var messages = _conversationManager.GetAllMessages();
            var toolResultMessages = messages.Where(m => m.GetType().Name.Contains("ToolResult")).ToList();
            Assert.IsTrue(toolResultMessages.Count > 0, "Should have tool result message");
            // The error should be captured in the tool result message
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
            private Func<StreamingLLMChunk[]>? _streamingChunksCallback;

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
                _streamingChunksCallback = null; // Clear callback when setting static chunks
            }

            public void SetStreamingChunksCallback(Func<StreamingLLMChunk[]> callback)
            {
                _streamingChunksCallback = callback;
                _streamingChunks = null; // Clear static chunks when setting callback
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

                // Use callback if set, otherwise use static chunks
                var chunks = _streamingChunksCallback?.Invoke() ?? _streamingChunks;

                if (chunks != null)
                {
                    foreach (var chunk in chunks)
                    {
                        await Task.Delay(1, cancellationToken); // Simulate async
                        yield return chunk;
                    }
                }
            }
        }

        /// <summary>
        /// Mock Tool Manager for testing
        /// </summary>
        private class MockToolManager : IToolManager
        {
            private ToolExecutionResult? _nextToolResult;
            private Exception? _nextToolException;
            private readonly List<LLMToolCall> _executedToolCalls = new();

            public bool WasToolExecuted => _executedToolCalls.Count > 0;
            public int ExecutedToolCount => _executedToolCalls.Count;
            public LLMToolCall? LastToolCall => _executedToolCalls.LastOrDefault();

            public IToolRegistry Registry => throw new NotImplementedException();

            public void SetNextToolResult(ToolExecutionResult result)
            {
                _nextToolResult = result;
                _nextToolException = null;
            }

            public void SetNextToolException(Exception exception)
            {
                _nextToolException = exception;
                _nextToolResult = null;
            }

            public Task<ToolExecutionResult> ExecuteToolCallAsync(LLMToolCall toolCall, CancellationToken cancellationToken = default)
            {
                _executedToolCalls.Add(toolCall);

                if (_nextToolException != null)
                    throw _nextToolException;

                return Task.FromResult(_nextToolResult ?? ToolExecutionResult.Success("Default result", TimeSpan.Zero));
            }

            public List<LLMTool> GetLLMToolDefinitions()
            {
                return new List<LLMTool>();
            }
        }

        #endregion
    }
}
