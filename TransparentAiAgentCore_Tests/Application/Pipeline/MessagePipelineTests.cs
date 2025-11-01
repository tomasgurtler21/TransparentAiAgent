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
