using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class UserMessageTests
{
    [TestMethod]
    public void UserMessage_NullContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new UserMessage(null!));
    }

    [TestMethod]
    public void UserMessage_EmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new UserMessage(""));
    }

    [TestMethod]
    public void UserMessage_WhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new UserMessage("   "));
    }

    [TestMethod]
    public void UserMessage_ValidContent_DefaultsToInContext()
    {
        // Act
        var message = new UserMessage("Hello");

        // Assert
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    [TestMethod]
    public void UserMessage_GeneratesUniqueIds()
    {
        // Act
        var msg1 = new UserMessage("Hello");
        var msg2 = new UserMessage("World");

        // Assert
        Assert.AreNotEqual(msg1.Id, msg2.Id);
        Assert.AreNotEqual(Guid.Empty, msg1.Id);
        Assert.AreNotEqual(Guid.Empty, msg2.Id);
    }

    [TestMethod]
    public void UserMessage_ContextStatus_CanBeUpdated()
    {
        // Arrange
        var message = new UserMessage("Hello");

        // Act
        message.ContextStatus = MessageContextStatus.TruncatedFromContext;

        // Assert
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, message.ContextStatus);
    }
}
