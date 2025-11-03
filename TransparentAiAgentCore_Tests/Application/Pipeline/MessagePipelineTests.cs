using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.Enums;

namespace TransparentAiAgentCore_Tests.Application.Pipeline
{
    [TestClass]
    public class MessagePipelineTests
    {
        private MessagePipeline _pipeline;

        [TestInitialize]
        public void Setup()
        {
            _pipeline = new MessagePipeline();
        }

        #region ConvertToLLMMessage Tests

        [TestMethod]
        public void ConvertToLLMMessage_NullMessage_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                _pipeline.ConvertToLLMMessage(null!));
        }

        [TestMethod]
        public void ConvertToLLMMessage_UserMessage_ConvertsCorrectly()
        {
            var userMessage = new UserMessage("Hello, AI!");

            var llmMessage = _pipeline.ConvertToLLMMessage(userMessage);

            Assert.AreEqual("user", llmMessage.Role);
            Assert.AreEqual("Hello, AI!", llmMessage.Content);
            Assert.IsNull(llmMessage.ToolCalls);
            Assert.IsNull(llmMessage.ToolCallId);
        }

        [TestMethod]
        public void ConvertToLLMMessage_AssistantMessage_ConvertsCorrectly()
        {
            var assistantMessage = new AssistantMessage("Hello, human!");

            var llmMessage = _pipeline.ConvertToLLMMessage(assistantMessage);

            Assert.AreEqual("assistant", llmMessage.Role);
            Assert.AreEqual("Hello, human!", llmMessage.Content);
            Assert.IsNull(llmMessage.ToolCalls);
            Assert.IsNull(llmMessage.ToolCallId);
        }

        [TestMethod]
        public void ConvertToLLMMessage_SystemMessage_ConvertsCorrectly()
        {
            var systemMessage = new SystemMessage("You are a helpful assistant.");

            var llmMessage = _pipeline.ConvertToLLMMessage(systemMessage);

            Assert.AreEqual("system", llmMessage.Role);
            Assert.AreEqual("You are a helpful assistant.", llmMessage.Content);
            Assert.IsNull(llmMessage.ToolCalls);
            Assert.IsNull(llmMessage.ToolCallId);
        }

        [TestMethod]
        public void ConvertToLLMMessage_AssistantToolCallMessage_ConvertsCorrectly()
        {
            var toolCall = new ToolCall("call_123", "get_weather", "{\"city\":\"Prague\"}");
            var toolCallMessage = new AssistantToolCallMessage("Calling weather API", new List<ToolCall> { toolCall });

            var llmMessage = _pipeline.ConvertToLLMMessage(toolCallMessage);

            Assert.AreEqual("assistant", llmMessage.Role);
            Assert.AreEqual("Calling weather API", llmMessage.Content);
            Assert.IsNotNull(llmMessage.ToolCalls);
            Assert.AreEqual(1, llmMessage.ToolCalls.Count);
            Assert.AreEqual("call_123", llmMessage.ToolCalls[0].Id);
            Assert.AreEqual("get_weather", llmMessage.ToolCalls[0].Name);
            Assert.AreEqual("{\"city\":\"Prague\"}", llmMessage.ToolCalls[0].Arguments);
        }

        [TestMethod]
        public void ConvertToLLMMessage_ToolResultMessage_ConvertsCorrectly()
        {
            var toolResultMessage = new ToolResultMessage("call_123", "get_weather", "{\"temperature\":20}", true);

            var llmMessage = _pipeline.ConvertToLLMMessage(toolResultMessage);

            Assert.AreEqual("tool", llmMessage.Role);
            Assert.AreEqual("{\"temperature\":20}", llmMessage.Content);
            Assert.AreEqual("call_123", llmMessage.ToolCallId);
        }

        [TestMethod]
        public void ConvertToLLMMessage_UnknownMessageType_ThrowsAgentException()
        {
            var unknownMessage = new TestMessage(); // Custom message type not in switch

            Assert.ThrowsException<AgentException>(() =>
                _pipeline.ConvertToLLMMessage(unknownMessage));
        }

        // ========================================
        // EMPTY CONTENT CONVERSION TESTS
        // ========================================
        // These tests ensure conversion works correctly with empty content
        // which is valid for assistant messages with tool calls

        [TestMethod]
        public void ConvertToLLMMessage_AssistantMessage_EmptyContent_ConvertsSuccessfully()
        {
            // Arrange - Assistant message with empty content (valid for tool-call-only responses)
            var assistantMessage = new AssistantMessage("");

            // Act
            var llmMessage = _pipeline.ConvertToLLMMessage(assistantMessage);

            // Assert
            Assert.AreEqual("assistant", llmMessage.Role);
            Assert.AreEqual("", llmMessage.Content);
            Assert.IsNull(llmMessage.ToolCalls);
        }

        [TestMethod]
        public void ConvertToLLMMessage_AssistantToolCallMessage_EmptyContent_ConvertsSuccessfully()
        {
            // Arrange - Tool call message with empty content (common when LLM only calls tools)
            var toolCall = new ToolCall("call-1", "search", "{\"query\":\"test\"}");
            var toolCallMessage = new AssistantToolCallMessage("", new List<ToolCall> { toolCall });

            // Act
            var llmMessage = _pipeline.ConvertToLLMMessage(toolCallMessage);

            // Assert
            Assert.AreEqual("assistant", llmMessage.Role);
            Assert.AreEqual("", llmMessage.Content);
            Assert.IsNotNull(llmMessage.ToolCalls);
            Assert.AreEqual(1, llmMessage.ToolCalls.Count);
        }

        [TestMethod]
        public void ConvertToLLMMessages_MixedEmptyAndNonEmptyContent_ConvertsAll()
        {
            // Arrange - Realistic scenario: conversation with mix of regular and tool-call messages
            var messages = new List<IMessage>
            {
                new UserMessage("Search for weather"),
                new AssistantToolCallMessage("", new List<ToolCall>
                {
                    new ToolCall("c1", "search", "{}")
                }),
                new ToolResultMessage("c1", "search", "{\"temp\":20}", true),
                new AssistantMessage("The temperature is 20°C")
            };

            // Act
            var llmMessages = _pipeline.ConvertToLLMMessages(messages);

            // Assert
            Assert.AreEqual(4, llmMessages.Count);
            Assert.AreEqual("user", llmMessages[0].Role);
            Assert.AreEqual("assistant", llmMessages[1].Role);
            Assert.AreEqual("", llmMessages[1].Content); // Empty content
            Assert.IsNotNull(llmMessages[1].ToolCalls);
            Assert.AreEqual("tool", llmMessages[2].Role);
            Assert.AreEqual("assistant", llmMessages[3].Role);
        }

        [TestMethod]
        public void ConvertToLLMMessage_RoundTrip_AssistantMessageWithEmptyContent()
        {
            // Arrange - Test round-trip: LLMResponse → AssistantMessage → LLMMessage
            var llmResponse = new LLMResponse("", new List<LLMToolCall>
            {
                new LLMToolCall("c1", "tool", "{}")
            });

            // Act - Simulate what happens in AgentOrchestrator
            // 1. Create domain message from response (handled in AgentOrchestrator)
            var toolCalls = llmResponse.ToolCalls!.Select(tc =>
                new ToolCall(tc.Id, tc.Name, tc.Arguments)).ToList();
            var domainMessage = new AssistantToolCallMessage(llmResponse.Content, toolCalls);

            // 2. Convert back to LLM message for next request
            var llmMessage = _pipeline.ConvertToLLMMessage(domainMessage);

            // Assert - Should preserve empty content
            Assert.AreEqual("assistant", llmMessage.Role);
            Assert.AreEqual("", llmMessage.Content);
            Assert.IsNotNull(llmMessage.ToolCalls);
            Assert.AreEqual(1, llmMessage.ToolCalls.Count);
        }

        #endregion

        #region ConvertToLLMMessages Tests

        [TestMethod]
        public void ConvertToLLMMessages_NullMessages_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                _pipeline.ConvertToLLMMessages(null!));
        }

        [TestMethod]
        public void ConvertToLLMMessages_EmptyList_ReturnsEmptyList()
        {
            var result = _pipeline.ConvertToLLMMessages(new List<IMessage>());

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void ConvertToLLMMessages_MultipleMessages_ConvertsAllInOrder()
        {
            var messages = new List<IMessage>
            {
                new SystemMessage("System prompt"),
                new UserMessage("Hello"),
                new AssistantMessage("Hi there!")
            };

            var llmMessages = _pipeline.ConvertToLLMMessages(messages);

            Assert.AreEqual(3, llmMessages.Count);
            Assert.AreEqual("system", llmMessages[0].Role);
            Assert.AreEqual("System prompt", llmMessages[0].Content);
            Assert.AreEqual("user", llmMessages[1].Role);
            Assert.AreEqual("Hello", llmMessages[1].Content);
            Assert.AreEqual("assistant", llmMessages[2].Role);
            Assert.AreEqual("Hi there!", llmMessages[2].Content);
        }

        #endregion

        #region ConvertToDomainMessage Tests

        [TestMethod]
        public void ConvertToDomainMessage_NullResponse_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                _pipeline.ConvertToDomainMessage(null!));
        }

        [TestMethod]
        public void ConvertToDomainMessage_TextResponse_CreatesAssistantMessage()
        {
            var llmResponse = new LLMResponse("This is a text response");

            var domainMessage = _pipeline.ConvertToDomainMessage(llmResponse);

            Assert.IsInstanceOfType(domainMessage, typeof(AssistantMessage));
            Assert.AreEqual("This is a text response", domainMessage.Content);
        }

        // Note: ConvertToDomainMessage no longer handles tool calls - they are handled in AgentOrchestrator
        // This test is removed as the functionality has been refactored

        [TestMethod]
        public void ConvertToDomainMessage_PreservesContentCorrectly()
        {
            var llmResponse = new LLMResponse("Detailed explanation here");

            var domainMessage = _pipeline.ConvertToDomainMessage(llmResponse);

            Assert.AreEqual("Detailed explanation here", domainMessage.Content);
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Test message type for testing unknown message handling
        /// </summary>
        private class TestMessage : IMessage
        {
            public Guid Id => Guid.NewGuid();
            public MessageRole Role => MessageRole.User;
            public string Content => "Test";
            public DateTime Timestamp => DateTime.UtcNow;
            public MessageContextStatus ContextStatus { get; set; }
        }

        #endregion
    }
}
