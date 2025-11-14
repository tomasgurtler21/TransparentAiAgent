using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class LlmToolCallMessageTests
{
    [TestMethod]
    public void Constructor_ValidToolCalls_SetsProperties()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "tool1", "{}")
        };

        // Act
        var msg = new LlmToolCallMessage("", toolCalls);

        // Assert
        Assert.AreEqual("Llm.ToolCall", msg.MessageTypeDiscriminator);
        Assert.AreEqual(1, msg.ToolCalls.Count);
        Assert.AreEqual("id1", msg.ToolCalls[0].Id);
        Assert.AreEqual("tool1", msg.ToolCalls[0].Name);
    }

    [TestMethod]
    public void Constructor_NullToolCalls_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LlmToolCallMessage("", null!));
    }

    [TestMethod]
    public void Constructor_EmptyToolCalls_ThrowsArgumentException()
    {
        // Arrange
        var emptyToolCalls = new List<ToolCall>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new LlmToolCallMessage("", emptyToolCalls));
    }

    [TestMethod]
    public void Role_IsAssistant()
    {
        // Arrange
        var toolCalls = new List<ToolCall> { new ToolCall("id1", "tool1", "{}") };

        // Act
        var msg = new LlmToolCallMessage("", toolCalls);

        // Assert
        Assert.AreEqual(MessageRole.Assistant, msg.Role);
    }

    [TestMethod]
    public void Constructor_WithContent_SetsContent()
    {
        // Arrange
        var toolCalls = new List<ToolCall> { new ToolCall("id1", "tool1", "{}") };

        // Act
        var msg = new LlmToolCallMessage("Some text with tool call", toolCalls);

        // Assert
        Assert.AreEqual("Some text with tool call", msg.Content);
    }

    [TestMethod]
    public void Constructor_NullContent_SetsEmptyContent()
    {
        // Arrange
        var toolCalls = new List<ToolCall> { new ToolCall("id1", "tool1", "{}") };

        // Act
        var msg = new LlmToolCallMessage(null!, toolCalls);

        // Assert
        Assert.AreEqual("", msg.Content);
    }

    [TestMethod]
    public void ToolCalls_IsReadOnly()
    {
        // Arrange
        var toolCalls = new List<ToolCall> { new ToolCall("id1", "tool1", "{}") };
        var msg = new LlmToolCallMessage("", toolCalls);

        // Act & Assert
        Assert.IsInstanceOfType(msg.ToolCalls, typeof(IReadOnlyList<ToolCall>));
    }

    [TestMethod]
    public void Constructor_MultipleToolCalls_StoresAllCorrectly()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new ToolCall("id1", "tool1", "{}"),
            new ToolCall("id2", "tool2", "{\"param\":\"value\"}")
        };

        // Act
        var msg = new LlmToolCallMessage("", toolCalls);

        // Assert
        Assert.AreEqual(2, msg.ToolCalls.Count);
        Assert.AreEqual("id1", msg.ToolCalls[0].Id);
        Assert.AreEqual("id2", msg.ToolCalls[1].Id);
    }
}
