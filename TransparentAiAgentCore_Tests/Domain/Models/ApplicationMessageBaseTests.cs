using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class ApplicationMessageBaseTests
{
    // Note: No concrete ApplicationMessage types exist yet.
    // These tests will be populated when we create ScenarioUserMessage in Session 1.4

    [TestMethod]
    public void ApplicationMessage_AbstractClassExists()
    {
        // This test verifies the abstract class exists and has the right interface
        var type = typeof(ApplicationMessage);

        // Assert
        Assert.IsTrue(type.IsAbstract);
        Assert.IsTrue(typeof(IMessage).IsAssignableFrom(type));
    }
}
