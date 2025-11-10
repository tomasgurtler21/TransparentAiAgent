using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Infrastructure.Scenarios;

namespace TransparentAiAgentCore_Tests.Infrastructure.Scenarios;

/// <summary>
/// Tests for JsonScenarioLoader - focuses on meaningful behavior:
/// - JSON deserialization and validation
/// - Error handling for invalid JSON
/// - File I/O error handling
/// - Directory scanning and batch loading
/// </summary>
[TestClass]
public class JsonScenarioLoaderTests
{
    private static readonly string TestDataDirectory = Path.Combine(
        Path.GetTempPath(),
        "TransparentAiAgent_Tests",
        Guid.NewGuid().ToString()
    );

    [TestInitialize]
    public void Setup()
    {
        // Create test directory
        Directory.CreateDirectory(TestDataDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test directory
        if (Directory.Exists(TestDataDirectory))
        {
            Directory.Delete(TestDataDirectory, recursive: true);
        }
    }

    #region LoadFromFileAsync Tests

    [TestMethod]
    public async Task LoadFromFileAsync_ValidJson_ReturnsScenarioDefinition()
    {
        // Arrange
        var validJson = """
        {
          "id": "test-scenario",
          "name": "Test Scenario",
          "description": "A test scenario",
          "category": "test",
          "difficulty": "beginner",
          "estimatedDurationSeconds": 60,
          "steps": [
            {
              "type": "auto_message",
              "content": "Hello",
              "delayMs": 1000
            },
            {
              "type": "wait_for_response"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "valid.json");
        await File.WriteAllTextAsync(filePath, validJson);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test-scenario", result.Id);
        Assert.AreEqual("Test Scenario", result.Name);
        Assert.AreEqual("A test scenario", result.Description);
        Assert.AreEqual("test", result.Category);
        Assert.AreEqual("beginner", result.Difficulty);
        Assert.AreEqual(60, result.EstimatedDurationSeconds);
        Assert.AreEqual(2, result.Steps.Count);
        Assert.AreEqual(ScenarioStepType.AutoMessage, result.Steps[0].Type);
        Assert.AreEqual("Hello", result.Steps[0].Content);
        Assert.AreEqual(1000, result.Steps[0].DelayMs);
        Assert.AreEqual(ScenarioStepType.WaitForResponse, result.Steps[1].Type);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_MinimalValidJson_ReturnsScenarioDefinition()
    {
        // Arrange - Only required fields
        var minimalJson = """
        {
          "id": "minimal",
          "name": "Minimal Scenario",
          "steps": [
            {
              "type": "completion_message",
              "content": "Done"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "minimal.json");
        await File.WriteAllTextAsync(filePath, minimalJson);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("minimal", result.Id);
        Assert.AreEqual("Minimal Scenario", result.Name);
        Assert.IsNull(result.Description);
        Assert.IsNull(result.Category);
        Assert.IsNull(result.Difficulty);
        Assert.IsNull(result.EstimatedDurationSeconds);
        Assert.AreEqual(1, result.Steps.Count);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var filePath = Path.Combine(TestDataDirectory, "invalid.json");
        await File.WriteAllTextAsync(filePath, invalidJson);
        var loader = new JsonScenarioLoader();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<JsonException>(() => loader.LoadFromFileAsync(filePath));
    }

    [TestMethod]
    public async Task LoadFromFileAsync_MissingRequiredField_ThrowsArgumentException()
    {
        // Arrange - Missing "name" field
        var jsonMissingName = """
        {
          "id": "test",
          "steps": [
            {
              "type": "auto_message",
              "content": "Hello"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "missing-name.json");
        await File.WriteAllTextAsync(filePath, jsonMissingName);
        var loader = new JsonScenarioLoader();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => loader.LoadFromFileAsync(filePath));
    }

    [TestMethod]
    public async Task LoadFromFileAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(TestDataDirectory, "does-not-exist.json");
        var loader = new JsonScenarioLoader();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => loader.LoadFromFileAsync(nonExistentPath));
    }

    [TestMethod]
    public async Task LoadFromFileAsync_AllStepTypes_MapsCorrectly()
    {
        // Arrange - Test all 4 step types
        var allStepTypesJson = """
        {
          "id": "all-steps",
          "name": "All Step Types",
          "steps": [
            {
              "type": "auto_message",
              "content": "Auto message"
            },
            {
              "type": "wait_for_response"
            },
            {
              "type": "agent_prompt",
              "content": "Agent prompt"
            },
            {
              "type": "completion_message",
              "content": "Completion"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "all-steps.json");
        await File.WriteAllTextAsync(filePath, allStepTypesJson);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(4, result.Steps.Count);
        Assert.AreEqual(ScenarioStepType.AutoMessage, result.Steps[0].Type);
        Assert.AreEqual(ScenarioStepType.WaitForResponse, result.Steps[1].Type);
        Assert.AreEqual(ScenarioStepType.AgentPrompt, result.Steps[2].Type);
        Assert.AreEqual(ScenarioStepType.CompletionMessage, result.Steps[3].Type);
    }

    #endregion

    #region LoadAllFromDirectoryAsync Tests

    [TestMethod]
    public async Task LoadAllFromDirectoryAsync_MultipleValidFiles_ReturnsAllScenarios()
    {
        // Arrange
        var json1 = """
        {
          "id": "scenario-1",
          "name": "Scenario 1",
          "steps": [{"type": "auto_message", "content": "Test"}]
        }
        """;
        var json2 = """
        {
          "id": "scenario-2",
          "name": "Scenario 2",
          "steps": [{"type": "completion_message", "content": "Done"}]
        }
        """;
        await File.WriteAllTextAsync(Path.Combine(TestDataDirectory, "scenario1.json"), json1);
        await File.WriteAllTextAsync(Path.Combine(TestDataDirectory, "scenario2.json"), json2);
        var loader = new JsonScenarioLoader();

        // Act
        var results = await loader.LoadAllFromDirectoryAsync(TestDataDirectory);

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(s => s.Id == "scenario-1"));
        Assert.IsTrue(results.Any(s => s.Id == "scenario-2"));
    }

    [TestMethod]
    public async Task LoadAllFromDirectoryAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var loader = new JsonScenarioLoader();

        // Act
        var results = await loader.LoadAllFromDirectoryAsync(TestDataDirectory);

        // Assert
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task LoadAllFromDirectoryAsync_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(TestDataDirectory, "does-not-exist");
        var loader = new JsonScenarioLoader();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(
            () => loader.LoadAllFromDirectoryAsync(nonExistentDir)
        );
    }

    [TestMethod]
    public async Task LoadAllFromDirectoryAsync_SkipsInvalidFiles_ReturnsValidScenariosOnly()
    {
        // Arrange
        var validJson = """
        {
          "id": "valid",
          "name": "Valid Scenario",
          "steps": [{"type": "auto_message", "content": "Test"}]
        }
        """;
        var invalidJson = "{ invalid }";
        await File.WriteAllTextAsync(Path.Combine(TestDataDirectory, "valid.json"), validJson);
        await File.WriteAllTextAsync(Path.Combine(TestDataDirectory, "invalid.json"), invalidJson);
        await File.WriteAllTextAsync(Path.Combine(TestDataDirectory, "readme.txt"), "Not a scenario");
        var loader = new JsonScenarioLoader();

        // Act
        var results = await loader.LoadAllFromDirectoryAsync(TestDataDirectory);

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("valid", results[0].Id);
    }

    #endregion

    #region Phase 10b: Advanced Step Types Tests

    [TestMethod]
    public async Task LoadFromFileAsync_ScenarioUserMessage_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "scenario_user_message",
              "content": "Hello, my name is John",
              "annotation": "The model will remember this for now",
              "delay": 1000
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "scenario-user-msg.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(1, result.Steps.Count);
        var step = result.Steps[0];
        Assert.AreEqual(ScenarioStepType.ScenarioUserMessage, step.Type);
        Assert.AreEqual("Hello, my name is John", step.Content);
        Assert.AreEqual("The model will remember this for now", step.Annotation);
        Assert.AreEqual(1000, step.DelayMs);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_ScenarioSystemMessage_ParsesVisibility()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "scenario_system_message",
              "content": "Teaching trigger",
              "visibleTo": "model_only"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "scenario-sys-msg.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(1, result.Steps.Count);
        var step = result.Steps[0];
        Assert.AreEqual(ScenarioStepType.ScenarioSystemMessage, step.Type);
        Assert.AreEqual("Teaching trigger", step.Content);
        Assert.AreEqual(MessageVisibility.ModelOnly, step.VisibleTo);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_WaitForCondition_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "wait_for_condition",
              "condition": "response_contains",
              "parameters": {
                "keywords": ["don't know", "cannot recall"],
                "timeout": 30000
              },
              "onTimeout": "fail",
              "annotation": "Waiting for confusion..."
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "wait-condition.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(1, result.Steps.Count);
        var step = result.Steps[0];
        Assert.AreEqual(ScenarioStepType.WaitForCondition, step.Type);
        Assert.AreEqual("response_contains", step.Condition);
        Assert.IsNotNull(step.ConditionParameters);
        Assert.AreEqual("fail", step.OnTimeout);
        Assert.AreEqual("Waiting for confusion...", step.Annotation);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_ApplyConfigOverlay_ParsesOverlay()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "apply_config_overlay",
              "overlay": {
                "messageLimit": 10,
                "systemPromptAddition": "Note: Teaching scenario"
              },
              "annotation": "Reducing message limit"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "apply-overlay.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(1, result.Steps.Count);
        var step = result.Steps[0];
        Assert.AreEqual(ScenarioStepType.ApplyConfigOverlay, step.Type);
        Assert.IsNotNull(step.ConfigOverlay);
        Assert.AreEqual(2, step.ConfigOverlay.Count);
        Assert.AreEqual("Reducing message limit", step.Annotation);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_RestoreConfigOverlay_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "restore_config_overlay",
              "annotation": "Message limit restored"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "restore-overlay.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(1, result.Steps.Count);
        var step = result.Steps[0];
        Assert.AreEqual(ScenarioStepType.RestoreConfigOverlay, step.Type);
        Assert.AreEqual("Message limit restored", step.Annotation);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_DisableEnableUserInput_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "disable_user_input"
            },
            {
              "type": "enable_user_input"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "user-input.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(2, result.Steps.Count);
        Assert.AreEqual(ScenarioStepType.DisableUserInput, result.Steps[0].Type);
        Assert.AreEqual(ScenarioStepType.EnableUserInput, result.Steps[1].Type);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_DelayStep_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "delay",
              "delay": 2000,
              "annotation": "Pausing to let you read"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "delay.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(1, result.Steps.Count);
        var step = result.Steps[0];
        Assert.AreEqual(ScenarioStepType.Delay, step.Type);
        Assert.AreEqual(2000, step.DelayMs);
        Assert.AreEqual("Pausing to let you read", step.Annotation);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_UIControlStep_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {
              "type": "ui_control",
              "tool": "ui_control_context_indicators",
              "arguments": {
                "visible": true,
                "highlighted": true
              },
              "annotation": "Highlighting context indicators"
            }
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "ui-control.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(1, result.Steps.Count);
        var step = result.Steps[0];
        Assert.AreEqual(ScenarioStepType.UIControl, step.Type);
        Assert.AreEqual("ui_control_context_indicators", step.UIControlTool);
        Assert.IsNotNull(step.UIControlArguments);
        Assert.AreEqual(true, step.UIControlArguments["visible"]);
        Assert.AreEqual(true, step.UIControlArguments["highlighted"]);
        Assert.AreEqual("Highlighting context indicators", step.Annotation);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_AllAdvancedStepTypes_MapsCorrectly()
    {
        // Arrange - Test all 9 advanced step types
        var allAdvancedStepsJson = """
        {
          "id": "all-advanced",
          "name": "All Advanced Steps",
          "steps": [
            {"type": "scenario_user_message", "content": "Test"},
            {"type": "scenario_system_message", "content": "Test"},
            {"type": "wait_for_condition", "condition": "response_contains"},
            {"type": "apply_config_overlay"},
            {"type": "restore_config_overlay"},
            {"type": "disable_user_input"},
            {"type": "enable_user_input"},
            {"type": "delay"},
            {"type": "ui_control", "tool": "test_tool"}
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "all-advanced.json");
        await File.WriteAllTextAsync(filePath, allAdvancedStepsJson);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(9, result.Steps.Count);
        Assert.AreEqual(ScenarioStepType.ScenarioUserMessage, result.Steps[0].Type);
        Assert.AreEqual(ScenarioStepType.ScenarioSystemMessage, result.Steps[1].Type);
        Assert.AreEqual(ScenarioStepType.WaitForCondition, result.Steps[2].Type);
        Assert.AreEqual(ScenarioStepType.ApplyConfigOverlay, result.Steps[3].Type);
        Assert.AreEqual(ScenarioStepType.RestoreConfigOverlay, result.Steps[4].Type);
        Assert.AreEqual(ScenarioStepType.DisableUserInput, result.Steps[5].Type);
        Assert.AreEqual(ScenarioStepType.EnableUserInput, result.Steps[6].Type);
        Assert.AreEqual(ScenarioStepType.Delay, result.Steps[7].Type);
        Assert.AreEqual(ScenarioStepType.UIControl, result.Steps[8].Type);
    }

    [TestMethod]
    public async Task LoadFromFileAsync_MessageVisibility_AllValues_ParseCorrectly()
    {
        // Arrange
        var json = """
        {
          "id": "test",
          "name": "Test",
          "steps": [
            {"type": "scenario_system_message", "content": "Model only", "visibleTo": "model_only"},
            {"type": "scenario_system_message", "content": "User only", "visibleTo": "user_only"},
            {"type": "scenario_system_message", "content": "Both", "visibleTo": "both"}
          ]
        }
        """;
        var filePath = Path.Combine(TestDataDirectory, "visibility.json");
        await File.WriteAllTextAsync(filePath, json);
        var loader = new JsonScenarioLoader();

        // Act
        var result = await loader.LoadFromFileAsync(filePath);

        // Assert
        Assert.AreEqual(3, result.Steps.Count);
        Assert.AreEqual(MessageVisibility.ModelOnly, result.Steps[0].VisibleTo);
        Assert.AreEqual(MessageVisibility.UserOnly, result.Steps[1].VisibleTo);
        Assert.AreEqual(MessageVisibility.Both, result.Steps[2].VisibleTo);
    }

    #endregion
}
