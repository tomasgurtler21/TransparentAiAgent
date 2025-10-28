using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TransparentAiAgentCore.Domain.LLM;

namespace TransparentAiAgentCore_Tests.Domain.LLM;

[TestClass]
public class LLMResponseTests
{
    [TestMethod]
    public void LLMResponse_EmptyContentAndNoToolCalls_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMResponse(""));
    }

    [TestMethod]
    public void LLMResponse_NullContentAndNoToolCalls_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMResponse(null!));
    }

    [TestMethod]
    public void LLMResponse_EmptyContentAndEmptyToolCalls_ThrowsArgumentException()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LLMResponse("", toolCalls));
    }

    [TestMethod]
    public void LLMResponse_EmptyContentWithToolCalls_IsValid()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>
        {
            new LLMToolCall("call-1", "get_weather", "{}")
        };

        // Act
        var response = new LLMResponse("", toolCalls);

        // Assert
        Assert.AreEqual("", response.Content);
        Assert.IsNotNull(response.ToolCalls);
        Assert.AreEqual(1, response.ToolCalls.Count);
    }

    [TestMethod]
    public void LLMResponse_ContentWithoutToolCalls_IsValid()
    {
        // Arrange & Act
        var response = new LLMResponse("Hello, how can I help you?");

        // Assert
        Assert.AreEqual("Hello, how can I help you?", response.Content);
        Assert.IsNull(response.ToolCalls);
        Assert.IsNull(response.FinishReason);
        Assert.IsNull(response.Usage);
    }

    [TestMethod]
    public void LLMResponse_FullResponse_IsValid()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>
        {
            new LLMToolCall("call-1", "get_weather", "{\"city\":\"Prague\"}")
        };
        var usage = new LLMUsage(150, 50);

        // Act
        var response = new LLMResponse(
            content: "Let me check the weather",
            toolCalls: toolCalls,
            finishReason: "tool_calls",
            usage: usage);

        // Assert
        Assert.AreEqual("Let me check the weather", response.Content);
        Assert.IsNotNull(response.ToolCalls);
        Assert.AreEqual(1, response.ToolCalls.Count);
        Assert.AreEqual("tool_calls", response.FinishReason);
        Assert.IsNotNull(response.Usage);
        Assert.AreEqual(200, response.Usage.TotalTokens);
    }

    [TestMethod]
    public void LLMResponse_NullContentWithToolCalls_DefaultsToEmptyString()
    {
        // Arrange
        var toolCalls = new List<LLMToolCall>
        {
            new LLMToolCall("call-1", "get_weather", "{}")
        };

        // Act
        var response = new LLMResponse(null, toolCalls);

        // Assert
        Assert.AreEqual("", response.Content);
        Assert.IsNotNull(response.ToolCalls);
    }
}
