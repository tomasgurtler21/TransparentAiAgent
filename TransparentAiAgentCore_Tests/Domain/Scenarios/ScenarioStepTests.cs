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
}
