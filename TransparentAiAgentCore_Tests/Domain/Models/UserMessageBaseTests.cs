using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class UserMessageBaseTests
{
    [TestMethod]
    public void UserMessage_DirectUserMessage_InheritsFromUserMessage()
    {
        // Arrange & Act
        var msg = new DirectUserMessage("Test");

        // Assert
        Assert.IsInstanceOfType(msg, typeof(UserMessage));
        Assert.IsInstanceOfType(msg, typeof(IMessage));
    }

    [TestMethod]
    public void UserMessage_Role_IsAlwaysUser()
    {
        // Arrange & Act
        var msg = new DirectUserMessage("Test");

        // Assert
        Assert.AreEqual(MessageRole.User, msg.Role);
    }
}
