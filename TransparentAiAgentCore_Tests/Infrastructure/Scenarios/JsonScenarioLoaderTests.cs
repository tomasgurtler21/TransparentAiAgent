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
}
