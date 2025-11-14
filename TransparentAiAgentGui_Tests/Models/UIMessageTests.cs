using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentGui.Models;

namespace TransparentAiAgentGui_Tests.Models;

[TestClass]
public class UIMessageTests
{
    [TestMethod]
    public void FromDomainMessage_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => UIMessage.FromDomainMessage(null!));
    }

    [TestMethod]
    public void FromDomainMessage_ValidMessage_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var domainMessage = new DirectUserMessage("Hello, world!");

        // Act
        var uiMessage = UIMessage.FromDomainMessage(domainMessage);

        // Assert
        Assert.AreEqual(domainMessage.Id, uiMessage.Id);
        Assert.AreEqual(MessageRole.User, uiMessage.Role);
        Assert.AreEqual("Hello, world!", uiMessage.Content);
        Assert.AreEqual(domainMessage.Timestamp, uiMessage.Timestamp);
        Assert.AreEqual(MessageContextStatus.InContext, uiMessage.ContextStatus);
    }

    [TestMethod]
    public void CssClass_UserRole_ReturnsMessageUser()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.User,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var cssClass = message.CssClass;

        // Assert
        Assert.AreEqual("message-user", cssClass);
    }

    [TestMethod]
    public void CssClass_AssistantRole_ReturnsMessageAssistant()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.Assistant,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var cssClass = message.CssClass;

        // Assert
        Assert.AreEqual("message-assistant", cssClass);
    }

    [TestMethod]
    public void CssClass_SystemRole_ReturnsMessageSystem()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.System,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var cssClass = message.CssClass;

        // Assert
        Assert.AreEqual("message-system", cssClass);
    }

    [TestMethod]
    public void CssClass_ToolRole_ReturnsMessageTool()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.Tool,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var cssClass = message.CssClass;

        // Assert
        Assert.AreEqual("message-tool", cssClass);
    }

    [TestMethod]
    public void ContextStatusIcon_InContext_ReturnsCheckMark()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.User,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var icon = message.ContextStatusIcon;

        // Assert
        Assert.AreEqual("✅", icon);
    }

    [TestMethod]
    public void ContextStatusIcon_TruncatedFromContext_ReturnsWarning()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.User,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.TruncatedFromContext
        };

        // Act
        var icon = message.ContextStatusIcon;

        // Assert
        Assert.AreEqual("⚠️", icon);
    }

    [TestMethod]
    public void ContextStatusTooltip_InContext_ReturnsCorrectTooltip()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.User,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.InContext
        };

        // Act
        var tooltip = message.ContextStatusTooltip;

        // Assert
        Assert.AreEqual("In LLM Context", tooltip);
    }

    [TestMethod]
    public void ContextStatusTooltip_TruncatedFromContext_ReturnsCorrectTooltip()
    {
        // Arrange
        var message = new UIMessage
        {
            Role = MessageRole.User,
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            ContextStatus = MessageContextStatus.TruncatedFromContext
        };

        // Act
        var tooltip = message.ContextStatusTooltip;

        // Assert
        Assert.AreEqual("Not in LLM Context (truncated)", tooltip);
    }
}
