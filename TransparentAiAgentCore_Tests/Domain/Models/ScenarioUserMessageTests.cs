using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Enums;
using System;

namespace TransparentAiAgentCore_Tests.Domain.Models
{
    [TestClass]
    public class ScenarioUserMessageTests
    {
        [TestMethod]
        public void Constructor_WithAnnotation_SetsProperties()
        {
            // Arrange & Act
            var msg = new ScenarioUserMessage("Hello", "Teaching step 1");

            // Assert
            Assert.AreEqual("Hello", msg.Content);
            Assert.AreEqual("Teaching step 1", msg.Annotation);
            Assert.AreEqual(MessageRole.User, msg.Role);
            Assert.AreEqual("Application.ScenarioUser", msg.MessageTypeDiscriminator);
            Assert.AreEqual("scenario_user_message", msg.MessageType);
        }

        [TestMethod]
        public void Constructor_WithoutAnnotation_AnnotationIsNull()
        {
            // Arrange & Act
            var msg = new ScenarioUserMessage("Hello");

            // Assert
            Assert.AreEqual("Hello", msg.Content);
            Assert.IsNull(msg.Annotation);
        }

        [TestMethod]
        public void Constructor_NullContent_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => new ScenarioUserMessage(null!));
        }

        [TestMethod]
        public void Constructor_EmptyContent_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => new ScenarioUserMessage(""));
        }

        [TestMethod]
        public void Constructor_WhitespaceContent_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => new ScenarioUserMessage("   "));
        }

        [TestMethod]
        public void Role_IsUser()
        {
            // Arrange & Act
            var msg = new ScenarioUserMessage("Test");

            // Assert
            Assert.AreEqual(MessageRole.User, msg.Role);
        }
    }
}
