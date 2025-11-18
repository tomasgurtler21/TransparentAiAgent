using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Enums;
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Infrastructure.Configuration;
using TransparentAiAgentCore.Infrastructure.Transparency;

namespace TransparentAiAgentCore_Tests.Application.Conversation;

/// <summary>
/// Tests for ConversationManager message restoration functionality.
/// Step 2 of CONFIG_RESTORATION_IMPLEMENTATION_PLAN.md (RED Phase)
/// </summary>
[TestClass]
public class ConversationManagerRestorationTests
{
    // Test 1: Messages are restored when limit increases
    [TestMethod]
    public void TruncateIfNeeded_LimitIncrease_RestoresTruncatedMessages()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var mockOverlay = new Mock<IConfigurationOverlay>();
        // Setup mock to return the base context window size initially (10)
        mockOverlay.Setup(o => o.GetValue("messageLimit", 10)).Returns(10);
        mockOverlay.Setup(o => o.GetValue<int>("messageLimit", 10)).Returns(10);

        var manager = new ConversationManager(10, mockTransparency.Object, mockOverlay.Object);

        // Add 20 messages (first 10 will be truncated when limit is 10)
        for (int i = 0; i < 20; i++)
        {
            manager.AddMessage(new DirectUserMessage($"Message {i}"));
        }

        // Verify initial truncation
        Assert.AreEqual(10, manager.InContextMessageCount, "Should have 10 messages in context");

        // Act: Increase limit to 15
        mockOverlay.Setup(o => o.GetValue("messageLimit", 10)).Returns(15);
        mockOverlay.Setup(o => o.GetValue<int>("messageLimit", 10)).Returns(15);
        manager.UpdateContextWindowSize(10); // Keep base size same, but overlay returns 15

        // Assert: 5 messages should be restored
        Assert.AreEqual(15, manager.InContextMessageCount, "Should restore 5 messages to reach limit of 15");
    }

    // Test 2: Restoration respects FIFO order (oldest truncated messages restored first)
    [TestMethod]
    public void TruncateIfNeeded_Restoration_RestoresOldestFirst()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(5, mockTransparency.Object, null);

        // Add 10 messages
        var messages = new List<DirectUserMessage>();
        for (int i = 0; i < 10; i++)
        {
            var msg = new DirectUserMessage($"Message {i}");
            messages.Add(msg);
            manager.AddMessage(msg);
        }

        // First 5 should be truncated
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, messages[0].ContextStatus);
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, messages[1].ContextStatus);
        Assert.AreEqual(MessageContextStatus.InContext, messages[5].ContextStatus);

        // Act: Increase limit to 7
        manager.UpdateContextWindowSize(7);

        // Assert: Messages 0 and 1 should be restored (oldest truncated first)
        Assert.AreEqual(MessageContextStatus.InContext, messages[0].ContextStatus, "Oldest message should be restored first");
        Assert.AreEqual(MessageContextStatus.InContext, messages[1].ContextStatus, "Second oldest should be restored next");
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, messages[2].ContextStatus, "This should still be truncated");
    }

    // Test 3: Restoration fires ContextStatusChanged events
    [TestMethod]
    public void TruncateIfNeeded_Restoration_FiresContextStatusChangedEvents()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(5, mockTransparency.Object, null);

        // Add 10 messages (5 will be truncated)
        for (int i = 0; i < 10; i++)
        {
            manager.AddMessage(new DirectUserMessage($"Message {i}"));
        }

        int eventCount = 0;
        manager.ContextStatusChanged += (sender, args) => eventCount++;

        // Act: Increase limit to 8 (should restore 3 messages)
        manager.UpdateContextWindowSize(8);

        // Assert: 3 ContextStatusChanged events should fire
        Assert.AreEqual(3, eventCount, "Should fire 3 events for 3 restored messages");
    }

    // Test 4: Restoration works with overlay changes
    [TestMethod]
    public void TruncateIfNeeded_OverlayPop_RestoresMessages()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var mockOverlay = new Mock<IConfigurationOverlay>();
        mockOverlay.Setup(o => o.GetValue<int>("messageLimit", 200)).Returns(8); // Initially 8

        var manager = new ConversationManager(200, mockTransparency.Object, mockOverlay.Object);

        // Add 20 messages with overlay limit of 8
        for (int i = 0; i < 20; i++)
        {
            manager.AddMessage(new DirectUserMessage($"Message {i}"));
        }

        Assert.AreEqual(8, manager.InContextMessageCount, "Overlay limit should be active");

        // Act: Simulate overlay pop (limit returns to 200)
        mockOverlay.Setup(o => o.GetValue<int>("messageLimit", 200)).Returns(200);
        mockOverlay.Raise(o => o.OverlayChanged += null, new ConfigurationChangedEventArgs(ChangeType.Pop, null));

        // Assert: All 20 messages should be restored
        Assert.AreEqual(20, manager.InContextMessageCount, "All messages should be restored after overlay pop");
    }

    // Test 5: No restoration needed when already at limit
    [TestMethod]
    public void TruncateIfNeeded_AlreadyAtLimit_NoChanges()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(10, mockTransparency.Object, null);

        // Add exactly 10 messages
        for (int i = 0; i < 10; i++)
        {
            manager.AddMessage(new DirectUserMessage($"Message {i}"));
        }

        int eventCount = 0;
        manager.ContextStatusChanged += (sender, args) => eventCount++;

        // Act: Update to same limit
        manager.UpdateContextWindowSize(10);

        // Assert: No events fired (no changes needed)
        Assert.AreEqual(0, eventCount, "No events should fire when already at limit");
    }

    // Test 6: Partial restoration when not enough truncated messages
    [TestMethod]
    public void TruncateIfNeeded_LimitExceedsTruncated_RestoresAllAvailable()
    {
        // Arrange
        var mockTransparency = new Mock<ITransparencyService>();
        var manager = new ConversationManager(5, mockTransparency.Object, null);

        // Add 7 messages (2 will be truncated)
        for (int i = 0; i < 7; i++)
        {
            manager.AddMessage(new DirectUserMessage($"Message {i}"));
        }

        Assert.AreEqual(5, manager.InContextMessageCount);

        // Act: Increase limit to 100 (but only 7 messages total)
        manager.UpdateContextWindowSize(100);

        // Assert: All 7 messages should be in context
        Assert.AreEqual(7, manager.InContextMessageCount, "Should restore all available messages");
    }
}
