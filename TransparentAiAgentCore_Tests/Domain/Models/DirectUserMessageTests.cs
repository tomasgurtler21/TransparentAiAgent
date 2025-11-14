using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class DirectUserMessageTests
{
    [TestMethod]
    public void Constructor_NullContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new DirectUserMessage(null!));
    }

    [TestMethod]
    public void Constructor_EmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new DirectUserMessage(""));
    }

    [TestMethod]
    public void Constructor_WhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new DirectUserMessage("   "));
    }

    [TestMethod]
    public void Constructor_ValidContent_SetsDiscriminator()
    {
        // Arrange & Act
        var msg = new DirectUserMessage("Test");

        // Assert
        Assert.AreEqual("User.Direct", msg.MessageTypeDiscriminator);
    }

    [TestMethod]
    public void Role_IsUser()
    {
        // Arrange & Act
        var msg = new DirectUserMessage("Test");

        // Assert
        Assert.AreEqual(MessageRole.User, msg.Role);
    }

    [TestMethod]
    public void Constructor_ValidContent_DefaultsToInContext()
    {
        // Arrange & Act
        var message = new DirectUserMessage("Hello");

        // Assert
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    [TestMethod]
    public void Constructor_GeneratesUniqueIds()
    {
        // Arrange & Act
        var msg1 = new DirectUserMessage("Hello");
        var msg2 = new DirectUserMessage("World");

        // Assert
        Assert.AreNotEqual(msg1.Id, msg2.Id);
        Assert.AreNotEqual(Guid.Empty, msg1.Id);
        Assert.AreNotEqual(Guid.Empty, msg2.Id);
    }

    [TestMethod]
    public void ContextStatus_CanBeUpdated()
    {
        // Arrange
        var message = new DirectUserMessage("Hello");

        // Act
        message.ContextStatus = MessageContextStatus.TruncatedFromContext;

        // Assert
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, message.ContextStatus);
    }
}
