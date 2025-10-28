using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class SystemMessageTests
{
    [TestMethod]
    public void SystemMessage_NullContent_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => new SystemMessage(null!));
    }

    [TestMethod]
    public void SystemMessage_ValidContent_DefaultsToInContext()
    {
        var message = new SystemMessage("You are a helpful assistant");
        Assert.AreEqual(MessageContextStatus.InContext, message.ContextStatus);
    }

    [TestMethod]
    public void SystemMessage_GeneratesUniqueIds()
    {
        var msg1 = new SystemMessage("Prompt 1");
        var msg2 = new SystemMessage("Prompt 2");

        Assert.AreNotEqual(msg1.Id, msg2.Id);
        Assert.AreNotEqual(Guid.Empty, msg1.Id);
    }
}
