using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Infrastructure.Scenarios;
using TransparentAiAgentCore.Application.Localization;
using TransparentAiAgentCore.Infrastructure.Localization;

namespace TransparentAiAgentCore_Tests.Infrastructure.Scenarios;

[TestClass]
public class ScenarioRegistryTests
{
    private const string TestScenariosPath = "./TestData/scenarios-registry";

    [TestInitialize]
    public void Setup()
    {
        // Create empty test scenarios directory
        Directory.CreateDirectory(TestScenariosPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(TestScenariosPath))
            Directory.Delete(TestScenariosPath, true);
    }

    private ScenarioRegistry CreateRegistry()
    {
        var languageService = new LanguageService();
        var translationService = new MockTranslationService();
        var loaderLogger = NullLogger<JsonScenarioLoader>.Instance;
        var registryLogger = NullLogger<ScenarioRegistry>.Instance;
        var loader = new JsonScenarioLoader(translationService, loaderLogger);
        return new ScenarioRegistry(loader, languageService, translationService, registryLogger, TestScenariosPath);
    }

    [TestMethod]
    public void GetAllScenarios_EmptyRegistry_ReturnsEmptyList()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var scenarios = registry.GetAllScenarios();

        // Assert
        Assert.IsNotNull(scenarios);
        Assert.AreEqual(0, scenarios.Count);
    }

    [TestMethod]
    public void GetScenarioById_EmptyRegistry_ReturnsNull()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var scenario = registry.GetScenarioById("nonexistent");

        // Assert
        Assert.IsNull(scenario);
    }

    [TestMethod]
    public void AddScenario_ValidScenario_CanBeRetrieved()
    {
        // Arrange
        var registry = CreateRegistry();
        var scenario = CreateTestScenario("test-1", "Test Scenario");

        // Act
        registry.AddScenario(scenario);
        var retrieved = registry.GetScenarioById("test-1");

        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("test-1", retrieved.Id);
        Assert.AreEqual("Test Scenario", retrieved.Name);
    }

    [TestMethod]
    public void AddScenario_NullScenario_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            registry.AddScenario(null!));
    }

    [TestMethod]
    public void AddScenario_DuplicateId_ThrowsArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();
        var scenario1 = CreateTestScenario("test-1", "First");
        var scenario2 = CreateTestScenario("test-1", "Second");

        // Act
        registry.AddScenario(scenario1);

        // Assert
        Assert.ThrowsException<ArgumentException>(() =>
            registry.AddScenario(scenario2));
    }

    [TestMethod]
    public void GetAllScenarios_MultipleScenarios_ReturnsAll()
    {
        // Arrange
        var registry = CreateRegistry();
        var scenario1 = CreateTestScenario("test-1", "First");
        var scenario2 = CreateTestScenario("test-2", "Second");
        var scenario3 = CreateTestScenario("test-3", "Third");

        registry.AddScenario(scenario1);
        registry.AddScenario(scenario2);
        registry.AddScenario(scenario3);

        // Act
        var scenarios = registry.GetAllScenarios();

        // Assert
        Assert.AreEqual(3, scenarios.Count);
    }

    [TestMethod]
    public void GetScenariosByCategory_MatchingCategory_ReturnsFiltered()
    {
        // Arrange
        var registry = CreateRegistry();
        var scenario1 = CreateTestScenario("test-1", "First", category: "context-management");
        var scenario2 = CreateTestScenario("test-2", "Second", category: "tools");
        var scenario3 = CreateTestScenario("test-3", "Third", category: "context-management");

        registry.AddScenario(scenario1);
        registry.AddScenario(scenario2);
        registry.AddScenario(scenario3);

        // Act
        var filtered = registry.GetScenariosByCategory("context-management");

        // Assert
        Assert.AreEqual(2, filtered.Count);
        Assert.IsTrue(filtered.All(s => s.Category == "context-management"));
    }

    [TestMethod]
    public void GetScenariosByDifficulty_MatchingDifficulty_ReturnsFiltered()
    {
        // Arrange
        var registry = CreateRegistry();
        var scenario1 = CreateTestScenario("test-1", "First", difficulty: "beginner");
        var scenario2 = CreateTestScenario("test-2", "Second", difficulty: "advanced");
        var scenario3 = CreateTestScenario("test-3", "Third", difficulty: "beginner");

        registry.AddScenario(scenario1);
        registry.AddScenario(scenario2);
        registry.AddScenario(scenario3);

        // Act
        var filtered = registry.GetScenariosByDifficulty("beginner");

        // Assert
        Assert.AreEqual(2, filtered.Count);
        Assert.IsTrue(filtered.All(s => s.Difficulty == "beginner"));
    }

    [TestMethod]
    public void GetScenariosByCategory_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var registry = CreateRegistry();
        var scenario = CreateTestScenario("test-1", "First", category: "tools");
        registry.AddScenario(scenario);

        // Act
        var filtered = registry.GetScenariosByCategory("nonexistent");

        // Assert
        Assert.IsNotNull(filtered);
        Assert.AreEqual(0, filtered.Count);
    }

    // Helper method to create test scenarios
    private ScenarioDefinition CreateTestScenario(
        string id,
        string name,
        string? category = null,
        string? difficulty = null)
    {
        var steps = new List<ScenarioStep>
        {
            new ScenarioStep(ScenarioStepType.AutoMessage, "Test message")
        };

        return new ScenarioDefinition(id, name, "Test description", steps,
            category: category, difficulty: difficulty);
    }

    // Mock implementation for tests
    private class MockTranslationService : ITranslationService
    {
        public string? GetTranslation(string key) => null; // Always fallback to inline content
        public Task LoadTranslationsAsync() => Task.CompletedTask;
    }
}
