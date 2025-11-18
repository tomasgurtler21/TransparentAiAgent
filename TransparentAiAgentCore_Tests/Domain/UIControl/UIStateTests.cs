using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore.Tests.Domain.UIControl;

/// <summary>
/// Tests for UIState domain model following Lean TDD principles.
/// Tests meaningful behavior: factory methods, immutability, defaults.
/// </summary>
[TestClass]
public class UIStateTests
{
    [TestMethod]
    public void DefaultNormalMode_CreatesAllControlsVisible()
    {
        // Arrange & Act
        var state = UIState.DefaultNormalMode();

        // Assert
        Assert.IsTrue(state.ChatFilter.FilterControlsVisible);
        Assert.IsTrue(state.ChatFilter.ShowSystemMessages);
        Assert.IsTrue(state.ChatFilter.ShowToolCalls);
        Assert.IsTrue(state.ChatFilter.ShowToolResults);
        Assert.IsTrue(state.ChatFilter.ShowUserMessages);
        Assert.IsTrue(state.ChatFilter.ShowAssistantMessages);
        Assert.IsTrue(state.ChatFilter.ShowTruncatedMessages);
        Assert.AreEqual(AppMode.Normal, state.CurrentMode);
    }

    [TestMethod]
    public void DefaultTeachingMode_HidesControlsAndMessages()
    {
        // Arrange & Act
        var state = UIState.DefaultTeachingMode();

        // Assert
        Assert.IsFalse(state.ChatFilter.FilterControlsVisible);
        Assert.IsFalse(state.ChatFilter.ShowSystemMessages);
        Assert.IsFalse(state.ChatFilter.ShowToolCalls);
        Assert.IsFalse(state.ChatFilter.ShowToolResults);
        Assert.IsTrue(state.ChatFilter.ShowUserMessages);
        Assert.IsTrue(state.ChatFilter.ShowAssistantMessages);
        Assert.IsTrue(state.ChatFilter.ShowTruncatedMessages);
        Assert.AreEqual(AppMode.Teaching, state.CurrentMode);
    }

    [TestMethod]
    public void UIState_IsImmutable_WithSyntaxCreatesNewInstance()
    {
        // Arrange
        var original = UIState.DefaultNormalMode();

        // Act
        var modified = original with
        {
            ChatFilter = original.ChatFilter with { ShowSystemMessages = false }
        };

        // Assert - Original unchanged (immutability)
        Assert.IsTrue(original.ChatFilter.ShowSystemMessages);

        // Assert - Modified has new value
        Assert.IsFalse(modified.ChatFilter.ShowSystemMessages);

        // Assert - Different instances
        Assert.AreNotSame(original, modified);
    }

    [TestMethod]
    public void ChatFilterState_DefaultConstructor_AllTrue()
    {
        // Arrange & Act
        var filter = new ChatFilterState();

        // Assert - Verify default values are all true (Normal Mode default)
        Assert.IsTrue(filter.ShowUserMessages);
        Assert.IsTrue(filter.ShowAssistantMessages);
        Assert.IsTrue(filter.ShowSystemMessages);
        Assert.IsTrue(filter.ShowToolCalls);
        Assert.IsTrue(filter.ShowToolResults);
        Assert.IsTrue(filter.ShowTruncatedMessages);
        Assert.IsTrue(filter.FilterControlsVisible);
    }

    [TestMethod]
    public void TransparencyViewerState_DefaultConstructor_VisibleWithNoFilters()
    {
        // Arrange & Act
        var viewer = new TransparencyViewerState();

        // Assert
        Assert.IsTrue(viewer.Visible);
        Assert.IsTrue(viewer.ShowTimestamps);
        Assert.IsNotNull(viewer.EventTypeFilters);
        Assert.AreEqual(0, viewer.EventTypeFilters.Count);
    }

    [TestMethod]
    public void ToolsPanelState_DefaultConstructor_VisibleWithNoExpanded()
    {
        // Arrange & Act
        var panel = new ToolsPanelState();

        // Assert
        Assert.IsTrue(panel.Visible);
        Assert.IsNotNull(panel.ExpandedTools);
        Assert.AreEqual(0, panel.ExpandedTools.Count);
        Assert.IsNull(panel.HighlightedTool);
    }

    [TestMethod]
    public void ContextIndicatorsState_DefaultConstructor_VisibleNotHighlighted()
    {
        // Arrange & Act
        var indicators = new ContextIndicatorsState();

        // Assert
        Assert.IsTrue(indicators.Visible);
        Assert.IsFalse(indicators.Highlighted);
    }

    [TestMethod]
    public void ConfigurationPageState_DefaultConstructor_DefaultsCorrect()
    {
        // Arrange & Act
        var configPage = new ConfigurationPageState();

        // Assert
        Assert.IsTrue(configPage.Visible);
        Assert.IsNull(configPage.HighlightedSection);
    }

    [TestMethod]
    public void AppMode_HasBothValues()
    {
        // Arrange & Act
        var normalMode = AppMode.Normal;
        var teachingMode = AppMode.Teaching;

        // Assert - Verify enum values exist (tests inheritance chain is correct)
        Assert.AreEqual(AppMode.Normal, normalMode);
        Assert.AreEqual(AppMode.Teaching, teachingMode);
        Assert.AreNotEqual(normalMode, teachingMode);
    }
}
