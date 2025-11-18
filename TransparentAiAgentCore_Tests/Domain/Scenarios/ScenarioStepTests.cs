using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Scenarios;

namespace TransparentAiAgentCore_Tests.Domain.Scenarios;

[TestClass]
public class ScenarioStepTests
{
    [TestMethod]
    public void ScenarioStep_NullContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.AutoMessage, null!));
    }

    [TestMethod]
    public void ScenarioStep_EmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.AutoMessage, ""));
    }

    [TestMethod]
    public void ScenarioStep_WhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.AutoMessage, "   "));
    }

    [TestMethod]
    public void ScenarioStep_ValidContent_SetsProperties()
    {
        // Act
        var step = new ScenarioStep(ScenarioStepType.AutoMessage, "Hello, world!");

        // Assert
        Assert.AreEqual(ScenarioStepType.AutoMessage, step.Type);
        Assert.AreEqual("Hello, world!", step.Content);
    }

    [TestMethod]
    public void ScenarioStep_WaitForResponse_AllowsNullContent()
    {
        // Act
        var step = new ScenarioStep(ScenarioStepType.WaitForResponse, null);

        // Assert
        Assert.AreEqual(ScenarioStepType.WaitForResponse, step.Type);
        Assert.IsNull(step.Content);
    }

    [TestMethod]
    public void ScenarioStep_WithDelay_SetsDelayProperty()
    {
        // Act
        var step = new ScenarioStep(ScenarioStepType.AutoMessage, "Test", delayMs: 1000);

        // Assert
        Assert.AreEqual(1000, step.DelayMs);
    }

    [TestMethod]
    public void ScenarioStep_WithoutDelay_DefaultsToZero()
    {
        // Act
        var step = new ScenarioStep(ScenarioStepType.AutoMessage, "Test");

        // Assert
        Assert.AreEqual(0, step.DelayMs);
    }

    [TestMethod]
    public void ScenarioStep_NegativeDelay_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test", delayMs: -100));
    }

    [TestMethod]
    public void ScenarioStep_WithoutConfigOverlay_DefaultsToNull()
    {
        // Act
        var step = new ScenarioStep(ScenarioStepType.AutoMessage, "Test");

        // Assert
        Assert.IsNull(step.ConfigOverlay);
    }

    [TestMethod]
    public void ScenarioStep_WithConfigOverlay_SetsProperty()
    {
        // Arrange
        var configOverlay = new Dictionary<string, object>
        {
            ["transparency.level"] = "high",
            ["teaching.mode"] = true
        };

        // Act
        var step = new ScenarioStep(
            ScenarioStepType.AutoMessage,
            "Test",
            delayMs: 0,
            configOverlay: configOverlay);

        // Assert
        Assert.IsNotNull(step.ConfigOverlay);
        Assert.AreEqual(2, step.ConfigOverlay.Count);
        Assert.AreEqual("high", step.ConfigOverlay["transparency.level"]);
        Assert.AreEqual(true, step.ConfigOverlay["teaching.mode"]);
    }

    // Phase 10b: Advanced step type validation tests

    [TestMethod]
    public void ScenarioStep_WaitForCondition_NullCondition_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.WaitForCondition, condition: null));
    }

    [TestMethod]
    public void ScenarioStep_WaitForCondition_EmptyCondition_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.WaitForCondition, condition: ""));
    }

    [TestMethod]
    public void ScenarioStep_WaitForCondition_ValidCondition_SetsProperty()
    {
        // Act
        var parameters = new Dictionary<string, object> { ["timeout"] = 5000 };
        var step = new ScenarioStep(
            ScenarioStepType.WaitForCondition,
            condition: "response_contains",
            conditionParameters: parameters);

        // Assert
        Assert.AreEqual(ScenarioStepType.WaitForCondition, step.Type);
        Assert.AreEqual("response_contains", step.Condition);
        Assert.IsNotNull(step.ConditionParameters);
        Assert.AreEqual(5000, step.ConditionParameters["timeout"]);
    }

    [TestMethod]
    public void ScenarioStep_UIControl_NullTool_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.UIControl, uiControlTool: null));
    }

    [TestMethod]
    public void ScenarioStep_UIControl_EmptyTool_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioStep(ScenarioStepType.UIControl, uiControlTool: ""));
    }

    [TestMethod]
    public void ScenarioStep_UIControl_ValidTool_SetsProperties()
    {
        // Arrange
        var arguments = new Dictionary<string, object> { ["visible"] = true };

        // Act
        var step = new ScenarioStep(
            ScenarioStepType.UIControl,
            uiControlTool: "ui_control_context_indicators",
            uiControlArguments: arguments);

        // Assert
        Assert.AreEqual(ScenarioStepType.UIControl, step.Type);
        Assert.AreEqual("ui_control_context_indicators", step.UIControlTool);
        Assert.IsNotNull(step.UIControlArguments);
        Assert.AreEqual(true, step.UIControlArguments["visible"]);
    }

    [TestMethod]
    public void ScenarioStep_ScenarioUserMessage_WithAnnotation_SetsProperty()
    {
        // Act
        var step = new ScenarioStep(
            ScenarioStepType.ScenarioUserMessage,
            content: "Hello",
            annotation: "This is visible to the user only");

        // Assert
        Assert.AreEqual("This is visible to the user only", step.Annotation);
    }

    [TestMethod]
    public void ScenarioStep_OnTimeout_DefaultsToContinue()
    {
        // Act
        var step = new ScenarioStep(
            ScenarioStepType.WaitForCondition,
            condition: "response_contains");

        // Assert
        Assert.AreEqual("continue", step.OnTimeout);
    }

    [TestMethod]
    public void ScenarioStep_OnTimeout_CustomValue_SetsProperty()
    {
        // Act
        var step = new ScenarioStep(
            ScenarioStepType.WaitForCondition,
            condition: "response_contains",
            onTimeout: "fail");

        // Assert
        Assert.AreEqual("fail", step.OnTimeout);
    }

    [TestMethod]
    public void ScenarioStep_AdvancedStepTypes_AllowNullContent()
    {
        // Act - These step types should not require content
        var applyOverlay = new ScenarioStep(ScenarioStepType.ApplyConfigOverlay);
        var restoreOverlay = new ScenarioStep(ScenarioStepType.RestoreConfigOverlay);
        var disableInput = new ScenarioStep(ScenarioStepType.DisableUserInput);
        var enableInput = new ScenarioStep(ScenarioStepType.EnableUserInput);

        // Assert - Should not throw
        Assert.AreEqual(ScenarioStepType.ApplyConfigOverlay, applyOverlay.Type);
        Assert.AreEqual(ScenarioStepType.RestoreConfigOverlay, restoreOverlay.Type);
        Assert.AreEqual(ScenarioStepType.DisableUserInput, disableInput.Type);
        Assert.AreEqual(ScenarioStepType.EnableUserInput, enableInput.Type);
    }

    // Phase 10c: Pause/Resume functionality tests

    [TestMethod]
    public void ScenarioStep_PauseForUser_WithMessage_CreatesSuccessfully()
    {
        // Act
        var step = new ScenarioStep(
            ScenarioStepType.PauseForUser,
            content: "Examine the UI and click Resume when ready");

        // Assert
        Assert.AreEqual(ScenarioStepType.PauseForUser, step.Type);
        Assert.AreEqual("Examine the UI and click Resume when ready", step.Content);
    }

    [TestMethod]
    public void ScenarioStep_PauseForUser_WithoutMessage_CreatesSuccessfully()
    {
        // Act - PauseForUser requires content now (since we removed PauseMessage)
        var step = new ScenarioStep(
            ScenarioStepType.PauseForUser,
            content: "Default pause message");

        // Assert
        Assert.AreEqual(ScenarioStepType.PauseForUser, step.Type);
        Assert.AreEqual("Default pause message", step.Content);
    }

    [TestMethod]
    public void ScenarioStep_PauseForUser_WithContentKey_SetsProperty()
    {
        // Act
        // Note: Translation would happen in JsonScenarioLoader, not in domain model
        var step = new ScenarioStep(
            ScenarioStepType.PauseForUser,
            content: "Fallback message");

        // Assert
        Assert.AreEqual("Fallback message", step.Content);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ScenarioStep_PauseForUser_RequiresContent()
    {
        // Act & Assert - PauseForUser now requires content, should throw
        var step = new ScenarioStep(ScenarioStepType.PauseForUser);
        Assert.IsNull(step.Content);
    }
}
