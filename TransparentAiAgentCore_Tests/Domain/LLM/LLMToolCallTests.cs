using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore_Tests.Domain.LLM;

[TestClass]
public class LLMToolCallTests
{
    [TestMethod]
    public void LLMToolCall_NullId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMToolCall(null!, "tool_name", "{}"));
    }

    [TestMethod]
    public void LLMToolCall_EmptyId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMToolCall("", "tool_name", "{}"));
    }

    [TestMethod]
    public void LLMToolCall_WhitespaceId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMToolCall("   ", "tool_name", "{}"));
    }

    [TestMethod]
    public void LLMToolCall_NullName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMToolCall("call-1", null!, "{}"));
    }

    [TestMethod]
    public void LLMToolCall_EmptyName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMToolCall("call-1", "", "{}"));
    }

    [TestMethod]
    public void LLMToolCall_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMToolCall("call-1", "   ", "{}"));
    }

    [TestMethod]
    public void LLMToolCall_NullArguments_DefaultsToEmptyJson()
    {
        // Arrange & Act
        var toolCall = new LLMToolCall("call-1", "get_weather", null!);

        // Assert
        Assert.AreEqual("call-1", toolCall.Id);
        Assert.AreEqual("get_weather", toolCall.Name);
        Assert.AreEqual("{}", toolCall.Arguments);
    }

    [TestMethod]
    public void LLMToolCall_ValidParameters_CreatesToolCall()
    {
        // Arrange & Act
        var toolCall = new LLMToolCall("call-123", "get_weather", "{\"city\":\"Prague\"}");

        // Assert
        Assert.AreEqual("call-123", toolCall.Id);
        Assert.AreEqual("get_weather", toolCall.Name);
        Assert.AreEqual("{\"city\":\"Prague\"}", toolCall.Arguments);
    }

    [TestMethod]
    public void LLMToolCall_EmptyArguments_PreservesEmptyString()
    {
        // Arrange & Act
        var toolCall = new LLMToolCall("call-1", "no_args_tool", "");

        // Assert
        Assert.AreEqual("call-1", toolCall.Id);
        Assert.AreEqual("no_args_tool", toolCall.Name);
        Assert.AreEqual("", toolCall.Arguments);
    }
}
