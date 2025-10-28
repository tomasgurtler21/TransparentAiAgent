using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Exceptions;

[TestClass]
public class ExceptionHierarchyTests
{
    [TestMethod]
    public void AgentException_CanBeThrown()
    {
        Assert.ThrowsException<AgentException>(() => throw new AgentException("Test error"));
    }

    [TestMethod]
    public void AgentException_InheritsFromException()
    {
        try
        {
            throw new AgentException("Test");
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, typeof(Exception));
            Assert.IsInstanceOfType(ex, typeof(AgentException));
        }
    }

    [TestMethod]
    public void AgentException_PreservesMessage()
    {
        var exception = new AgentException("Test message");
        Assert.AreEqual("Test message", exception.Message);
    }

    [TestMethod]
    public void AgentException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("Inner");
        var exception = new AgentException("Outer", inner);

        Assert.AreEqual("Outer", exception.Message);
        Assert.AreSame(inner, exception.InnerException);
    }

    [TestMethod]
    public void ConfigurationException_InheritsFromAgentException()
    {
        try
        {
            throw new ConfigurationException("Config error");
        }
        catch (AgentException ex)
        {
            Assert.IsInstanceOfType(ex, typeof(ConfigurationException));
        }
    }

    [TestMethod]
    public void LLMException_InheritsFromAgentException()
    {
        try
        {
            throw new LLMException("LLM error");
        }
        catch (AgentException ex)
        {
            Assert.IsInstanceOfType(ex, typeof(LLMException));
        }
    }

    [TestMethod]
    public void MCPException_InheritsFromAgentException()
    {
        try
        {
            throw new MCPException("MCP error");
        }
        catch (AgentException ex)
        {
            Assert.IsInstanceOfType(ex, typeof(MCPException));
        }
    }

    [TestMethod]
    public void TransparencyException_InheritsFromAgentException()
    {
        try
        {
            throw new TransparencyException("Transparency error");
        }
        catch (AgentException ex)
        {
            Assert.IsInstanceOfType(ex, typeof(TransparencyException));
        }
    }
}
