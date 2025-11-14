using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using System;

namespace TransparentAiAgentCore_Tests.Domain.Models
{
    [TestClass]
    public class ScenarioAssistantMessageTests
    {
        [TestMethod]
        public void Constructor_WithAnnotation_SetsProperties()
        {
            // Arrange & Act
            var msg = new ScenarioAssistantMessage("Response", "Teaching response");

            // Assert
            Assert.AreEqual("Response", msg.Content);
            Assert.AreEqual("Teaching response", msg.Annotation);
            Assert.AreEqual(MessageRole.Assistant, msg.Role);
            Assert.AreEqual("Application.ScenarioAssistant", msg.MessageTypeDiscriminator);
            Assert.AreEqual("scenario_assistant_message", msg.MessageType);
        }

        [TestMethod]
        public void Constructor_WithoutAnnotation_AnnotationIsNull()
        {
            // Arrange & Act
            var msg = new ScenarioAssistantMessage("Response");

            // Assert
            Assert.AreEqual("Response", msg.Content);
            Assert.IsNull(msg.Annotation);
        }

        [TestMethod]
        public void Constructor_NullContent_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => new ScenarioAssistantMessage(null!));
        }

        [TestMethod]
        public void Constructor_EmptyContent_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => new ScenarioAssistantMessage(""));
        }

        [TestMethod]
        public void Constructor_WhitespaceContent_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => new ScenarioAssistantMessage("   "));
        }

        [TestMethod]
        public void Role_IsAssistant()
        {
            // Arrange & Act
            var msg = new ScenarioAssistantMessage("Response");

            // Assert
            Assert.AreEqual(MessageRole.Assistant, msg.Role);
        }
    }
}
