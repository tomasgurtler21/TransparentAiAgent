using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentGui.Models;

namespace TransparentAiAgentGui_Tests.Models;

/// <summary>
/// Tests for UIMessage conversion from domain message types.
/// Following TDD: Tests verify meaningful behavior of message type conversion.
/// </summary>
[TestClass]
public class UIMessageConversionTests
{
    #region DirectUserMessage Tests

    [TestMethod]
    public void FromDomainMessage_DirectUserMessage_SetsStandardUserDisplay()
    {
        // Arrange
        var domainMsg = new DirectUserMessage("Hello");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.AreEqual("Hello", uiMsg.Content);
        Assert.IsFalse(uiMsg.IsAutoMessage, "DirectUserMessage should not be marked as auto message");
        Assert.IsNull(uiMsg.Annotation, "DirectUserMessage should not have annotation");
        Assert.AreEqual(MessageRole.User, uiMsg.Role);
    }

    [TestMethod]
    public void FromDomainMessage_DirectUserMessage_IsNotToolCall()
    {
        // Arrange
        var domainMsg = new DirectUserMessage("Test");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsFalse(uiMsg.IsToolCall);
        Assert.IsFalse(uiMsg.IsToolResult);
    }

    #endregion

    #region ScenarioUserMessage Tests

    [TestMethod]
    public void FromDomainMessage_ScenarioUserMessage_SetsAutoMessageDisplay()
    {
        // Arrange
        var domainMsg = new ScenarioUserMessage("Hello", "Teaching step 1");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.AreEqual("Hello", uiMsg.Content);
        Assert.IsTrue(uiMsg.IsAutoMessage, "ScenarioUserMessage should be marked as auto message");
        Assert.AreEqual("Teaching step 1", uiMsg.Annotation, "Annotation should be set from ScenarioUserMessage");
        Assert.AreEqual(MessageRole.User, uiMsg.Role);
    }

    [TestMethod]
    public void FromDomainMessage_ScenarioUserMessage_WithoutAnnotation_SetsAutoMessage()
    {
        // Arrange
        var domainMsg = new ScenarioUserMessage("Hello");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsTrue(uiMsg.IsAutoMessage, "ScenarioUserMessage without annotation should still be marked as auto message");
        Assert.IsNull(uiMsg.Annotation);
    }

    [TestMethod]
    public void FromDomainMessage_ScenarioUserMessage_IsNotToolCall()
    {
        // Arrange
        var domainMsg = new ScenarioUserMessage("Test", "Teaching");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsFalse(uiMsg.IsToolCall);
        Assert.IsFalse(uiMsg.IsToolResult);
    }

    #endregion

    #region ScenarioAssistantMessage Tests

    [TestMethod]
    public void FromDomainMessage_ScenarioAssistantMessage_SetsAutoMessageDisplay()
    {
        // Arrange
        var domainMsg = new ScenarioAssistantMessage("Response", "Teaching response 1");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.AreEqual("Response", uiMsg.Content);
        Assert.IsTrue(uiMsg.IsAutoMessage, "ScenarioAssistantMessage should be marked as auto message");
        Assert.AreEqual("Teaching response 1", uiMsg.Annotation);
        Assert.AreEqual(MessageRole.Assistant, uiMsg.Role);
    }

    [TestMethod]
    public void FromDomainMessage_ScenarioAssistantMessage_IsNotToolCall()
    {
        // Arrange
        var domainMsg = new ScenarioAssistantMessage("Test", "Teaching");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsFalse(uiMsg.IsToolCall);
        Assert.IsFalse(uiMsg.IsToolResult);
    }

    #endregion

    #region LlmTextMessage Tests

    [TestMethod]
    public void FromDomainMessage_LlmTextMessage_SetsBasicAssistantDisplay()
    {
        // Arrange
        var domainMsg = new LlmTextMessage("AI Response");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.AreEqual("AI Response", uiMsg.Content);
        Assert.IsFalse(uiMsg.IsAutoMessage, "LlmTextMessage should not be marked as auto message");
        Assert.IsNull(uiMsg.Annotation);
        Assert.AreEqual(MessageRole.Assistant, uiMsg.Role);
    }

    [TestMethod]
    public void FromDomainMessage_LlmTextMessage_IsNotToolCall()
    {
        // Arrange
        var domainMsg = new LlmTextMessage("Test");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsFalse(uiMsg.IsToolCall);
        Assert.IsFalse(uiMsg.IsToolResult);
    }

    #endregion

    #region LlmToolCallMessage Tests

    [TestMethod]
    public void FromDomainMessage_LlmToolCallMessage_SetsToolCallDisplay()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "get_weather", "{\"city\":\"NYC\"}"),
            new ToolCall("id2", "calculate", "{\"x\":5}")
        };
        var domainMsg = new LlmToolCallMessage("", toolCalls);

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsTrue(uiMsg.IsToolCall, "LlmToolCallMessage should be marked as tool call");
        Assert.AreEqual(2, uiMsg.ToolCalls.Count);
        Assert.AreEqual("get_weather", uiMsg.ToolCalls[0].Name);
        Assert.AreEqual("id1", uiMsg.ToolCalls[0].Id);
        Assert.AreEqual("{\"city\":\"NYC\"}", uiMsg.ToolCalls[0].Arguments);
        Assert.AreEqual(MessageRole.Assistant, uiMsg.Role);
    }

    [TestMethod]
    public void FromDomainMessage_LlmToolCallMessage_IsNotAutoMessage()
    {
        // Arrange
        var toolCalls = new List<ToolCall> { new ToolCall("id1", "tool", "{}") };
        var domainMsg = new LlmToolCallMessage("", toolCalls);

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsFalse(uiMsg.IsAutoMessage);
        Assert.IsFalse(uiMsg.IsToolResult);
    }

    #endregion

    #region ToolResultMessage Tests

    [TestMethod]
    public void FromDomainMessage_ToolResultMessage_SetsToolResultDisplay()
    {
        // Arrange
        var toolCallId = "tool_call_123";
        var domainMsg = new ToolResultMessage(toolCallId, "get_weather", "{\"temp\":20}", false);

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsTrue(uiMsg.IsToolResult, "ToolResultMessage should be marked as tool result");
        Assert.AreEqual("get_weather", uiMsg.ToolName);
        Assert.IsTrue(uiMsg.ToolResultSuccess, "IsError=false should map to ToolResultSuccess=true");
        Assert.AreEqual(MessageRole.Tool, uiMsg.Role);
        Assert.AreEqual("{\"temp\":20}", uiMsg.Content);
    }

    [TestMethod]
    public void FromDomainMessage_ToolResultMessage_ErrorResult_SetsErrorFlag()
    {
        // Arrange
        var toolCallId = "tool_call_123";
        var domainMsg = new ToolResultMessage(toolCallId, "get_weather", "error data", true);

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsTrue(uiMsg.IsToolResult);
        Assert.IsFalse(uiMsg.ToolResultSuccess, "IsError=true should map to ToolResultSuccess=false");
    }

    [TestMethod]
    public void FromDomainMessage_ToolResultMessage_IsNotAutoMessage()
    {
        // Arrange
        var domainMsg = new ToolResultMessage("tool_call_123", "tool", "result", false);

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsFalse(uiMsg.IsAutoMessage);
        Assert.IsFalse(uiMsg.IsToolCall);
    }

    #endregion

    #region ToolErrorMessage Tests

    [TestMethod]
    public void FromDomainMessage_ToolErrorMessage_SetsToolErrorDisplay()
    {
        // Arrange
        var toolCallId = "tool_call_456";
        var exception = new InvalidOperationException("Test error");
        var domainMsg = new ToolErrorMessage(toolCallId, "get_weather", "Failed to connect", exception);

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsTrue(uiMsg.IsToolResult, "ToolErrorMessage should be marked as tool result");
        Assert.AreEqual("get_weather", uiMsg.ToolName);
        Assert.IsFalse(uiMsg.ToolResultSuccess, "ToolErrorMessage should have ToolResultSuccess=false");
        Assert.AreEqual("Failed to connect", uiMsg.ToolErrorMessage);
        Assert.AreEqual(MessageRole.Tool, uiMsg.Role);
    }

    [TestMethod]
    public void FromDomainMessage_ToolErrorMessage_WithoutException_StillSetsError()
    {
        // Arrange
        var toolCallId = "tool_call_456";
        var domainMsg = new ToolErrorMessage(toolCallId, "tool", "Error occurred");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsTrue(uiMsg.IsToolResult);
        Assert.IsFalse(uiMsg.ToolResultSuccess);
        Assert.AreEqual("Error occurred", uiMsg.ToolErrorMessage);
    }

    [TestMethod]
    public void FromDomainMessage_ToolErrorMessage_IsNotAutoMessage()
    {
        // Arrange
        var domainMsg = new ToolErrorMessage("tool_call_456", "tool", "error");

        // Act
        var uiMsg = UIMessage.FromDomainMessage(domainMsg);

        // Assert
        Assert.IsFalse(uiMsg.IsAutoMessage);
        Assert.IsFalse(uiMsg.IsToolCall);
    }

    #endregion
}
