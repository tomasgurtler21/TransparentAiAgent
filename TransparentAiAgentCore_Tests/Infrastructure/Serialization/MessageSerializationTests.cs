using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.Serialization;

namespace TransparentAiAgentCore_Tests.Infrastructure.Serialization
{
    [TestClass]
    public class MessageSerializationTests
    {
        private MessageSerializer _serializer = null!;

        [TestInitialize]
        public void Setup()
        {
            _serializer = new MessageSerializer();
        }

        [TestMethod]
        public void Serialize_DirectUserMessage_IncludesDiscriminator()
        {
            // Arrange
            var msg = new DirectUserMessage("Hello");

            // Act
            var json = _serializer.Serialize(msg);

            // Assert
            Assert.IsTrue(json.Contains("\"MessageTypeDiscriminator\":\"User.Direct\"") || json.Contains("\"MessageTypeDiscriminator\": \"User.Direct\""));
            Assert.IsTrue(json.Contains("\"Content\":\"Hello\"") || json.Contains("\"Content\": \"Hello\""));
        }

        [TestMethod]
        public void Serialize_ScenarioUserMessage_IncludesAnnotation()
        {
            // Arrange
            var msg = new ScenarioUserMessage("Hello", "Teaching");

            // Act
            var json = _serializer.Serialize(msg);

            // Assert
            Assert.IsTrue(json.Contains("\"Annotation\":\"Teaching\"") || json.Contains("\"Annotation\": \"Teaching\""));
        }

        [TestMethod]
        public void Serialize_NullMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _serializer.Serialize(null!));
        }

        [TestMethod]
        public void Deserialize_DirectUserMessage_RestoresCorrectType()
        {
            // Arrange
            var msg = new DirectUserMessage("Hello");
            var json = _serializer.Serialize(msg);

            // Act
            var deserialized = _serializer.Deserialize(json);

            // Assert
            Assert.IsInstanceOfType(deserialized, typeof(DirectUserMessage));
            Assert.AreEqual("Hello", deserialized.Content);
            Assert.AreEqual("User.Direct", deserialized.MessageTypeDiscriminator);
        }

        [TestMethod]
        public void Deserialize_ScenarioUserMessage_RestoresAnnotation()
        {
            // Arrange
            var msg = new ScenarioUserMessage("Hello", "Teaching");
            var json = _serializer.Serialize(msg);

            // Act
            var deserialized = _serializer.Deserialize(json);

            // Assert
            Assert.IsInstanceOfType(deserialized, typeof(ScenarioUserMessage));
            var scenarioMsg = (ScenarioUserMessage)deserialized;
            Assert.AreEqual("Hello", scenarioMsg.Content);
            Assert.AreEqual("Teaching", scenarioMsg.Annotation);
        }

        [TestMethod]
        public void Deserialize_UnknownDiscriminator_ThrowsInvalidOperationException()
        {
            // Arrange
            var json = "{\"MessageTypeDiscriminator\":\"Unknown.Type\"}";

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => _serializer.Deserialize(json));
        }

        [TestMethod]
        public void Deserialize_NullJson_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _serializer.Deserialize(null!));
        }

        [TestMethod]
        public void Deserialize_EmptyJson_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _serializer.Deserialize(string.Empty));
        }

        [TestMethod]
        public void RoundTrip_AllMessageTypes_PreservesData()
        {
            // Arrange - Create instances of all message types
            var messages = new List<IMessage>
            {
                new DirectUserMessage("User message content"),
                new ScenarioUserMessage("Scenario user content", "Annotation 1"),
                new ScenarioAssistantMessage("Scenario assistant content", "Annotation 2"),
                new LlmTextMessage("LLM text response"),
                new LlmToolCallMessage("", new List<ToolCall>
                {
                    new ToolCall("tool_call_001", "test_tool", "{\"param\":\"value\"}")
                }),
                new ToolResultMessage("tool_call_123", "tool_name", "Tool result content", false),
                new ToolErrorMessage("tool_call_456", "tool_name", "Error message"),
                new SystemMessage("System prompt content")
            };

            // Act & Assert - Round-trip each message
            foreach (var original in messages)
            {
                var json = _serializer.Serialize(original);
                var deserialized = _serializer.Deserialize(json);

                // Verify type matches
                Assert.AreEqual(original.GetType(), deserialized.GetType(),
                    $"Type mismatch for {original.GetType().Name}");

                // Verify discriminator matches
                Assert.AreEqual(original.MessageTypeDiscriminator, deserialized.MessageTypeDiscriminator,
                    $"Discriminator mismatch for {original.GetType().Name}");

                // Verify content matches (basic check - more specific checks below)
                Assert.AreEqual(original.Content, deserialized.Content,
                    $"Content mismatch for {original.GetType().Name}");
            }
        }

        [TestMethod]
        public void RoundTrip_LlmToolCallMessage_PreservesToolCalls()
        {
            // Arrange
            var toolCallId = Guid.NewGuid().ToString();
            var original = new LlmToolCallMessage("Optional text", new List<ToolCall>
            {
                new ToolCall(toolCallId, "get_weather", "{\"location\":\"NYC\"}")
            });

            // Act
            var json = _serializer.Serialize(original);
            var deserialized = _serializer.Deserialize(json) as LlmToolCallMessage;

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.ToolCalls.Count);
            Assert.AreEqual(toolCallId, deserialized.ToolCalls[0].Id);
            Assert.AreEqual("get_weather", deserialized.ToolCalls[0].Name);
        }

        [TestMethod]
        public void RoundTrip_ToolResultMessage_PreservesAllProperties()
        {
            // Arrange
            var toolCallId = "tool_call_123";
            var original = new ToolResultMessage(toolCallId, "test_tool", "Success result", false);

            // Act
            var json = _serializer.Serialize(original);
            var deserialized = _serializer.Deserialize(json) as ToolResultMessage;

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(toolCallId, deserialized.ToolCallId);
            Assert.AreEqual("test_tool", deserialized.ToolName);
            Assert.AreEqual("Success result", deserialized.Result);
            Assert.IsFalse(deserialized.IsError);
        }

        [TestMethod]
        public void RoundTrip_ToolErrorMessage_PreservesErrorDetails()
        {
            // Arrange
            var toolCallId = "tool_call_456";
            var exception = new InvalidOperationException("Test error");
            var original = new ToolErrorMessage(toolCallId, "test_tool", "Error occurred", exception);

            // Act
            var json = _serializer.Serialize(original);
            var deserialized = _serializer.Deserialize(json) as ToolErrorMessage;

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(toolCallId, deserialized.ToolCallId);
            Assert.AreEqual("test_tool", deserialized.ToolName);
            Assert.AreEqual("Error occurred", deserialized.ErrorMessage);
            // Note: Exception is not preserved through JSON serialization
        }
    }
}
