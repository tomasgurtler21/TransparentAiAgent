using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Scenarios;

namespace TransparentAiAgentCore_Tests.Domain.Scenarios;

[TestClass]
public class ScenarioDefinitionTests
{
    [TestMethod]
    public void ScenarioDefinition_NullId_ThrowsArgumentException()
    {
        // Arrange
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test")
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioDefinition(null!, "Test Scenario", "Description", steps));
    }

    [TestMethod]
    public void ScenarioDefinition_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test")
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioDefinition("", "Test Scenario", "Description", steps));
    }

    [TestMethod]
    public void ScenarioDefinition_NullName_ThrowsArgumentException()
    {
        // Arrange
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test")
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioDefinition("test-scenario", null!, "Description", steps));
    }

    [TestMethod]
    public void ScenarioDefinition_NullSteps_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new ScenarioDefinition("test-scenario", "Test Scenario", "Description", null!));
    }

    [TestMethod]
    public void ScenarioDefinition_EmptySteps_ThrowsArgumentException()
    {
        // Arrange
        var emptySteps = new List<ScenarioStep>();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioDefinition("test-scenario", "Test Scenario", "Description", emptySteps));
    }

    [TestMethod]
    public void ScenarioDefinition_ValidParameters_SetsProperties()
    {
        // Arrange
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Hello"),
            new ScenarioStep(ScenarioStepType.WaitForResponse, null)
        };

        // Act
        var scenario = new ScenarioDefinition(
            "test-scenario",
            "Test Scenario",
            "A test scenario",
            steps);

        // Assert
        Assert.AreEqual("test-scenario", scenario.Id);
        Assert.AreEqual("Test Scenario", scenario.Name);
        Assert.AreEqual("A test scenario", scenario.Description);
        Assert.AreEqual(2, scenario.Steps.Count);
    }

    [TestMethod]
    public void ScenarioDefinition_NullDescription_AllowsNull()
    {
        // Arrange
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test")
        };

        // Act
        var scenario = new ScenarioDefinition("test-scenario", "Test Scenario", null, steps);

        // Assert
        Assert.IsNull(scenario.Description);
    }

    [TestMethod]
    public void ScenarioDefinition_WithOptionalProperties_SetsAllProperties()
    {
        // Arrange
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test")
        };

        // Act
        var scenario = new ScenarioDefinition(
            "test-scenario",
            "Test Scenario",
            "Description",
            steps,
            category: "context-management",
            difficulty: "beginner",
            estimatedDurationSeconds: 180);

        // Assert
        Assert.AreEqual("context-management", scenario.Category);
        Assert.AreEqual("beginner", scenario.Difficulty);
        Assert.AreEqual(180, scenario.EstimatedDurationSeconds);
    }

    [TestMethod]
    public void ScenarioDefinition_NegativeDuration_ThrowsArgumentException()
    {
        // Arrange
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test")
        };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
            new ScenarioDefinition(
                "test-scenario",
                "Test Scenario",
                "Description",
                steps,
                estimatedDurationSeconds: -10));
    }
}
