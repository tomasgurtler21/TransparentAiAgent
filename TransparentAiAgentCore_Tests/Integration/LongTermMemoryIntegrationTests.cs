using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Infrastructure.Memory;
using TransparentAiAgentCore.Infrastructure.DataPath;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;

namespace TransparentAiAgentCore_Tests.Integration;

/// <summary>
/// Integration tests for Long-Term Memory feature.
/// Tests that all components work together correctly.
/// </summary>
[TestClass]
public class LongTermMemoryIntegrationTests
{
    private string? _testDirectory;

    [TestInitialize]
    public void TestInitialize()
    {
        // Create unique temp directory for each test
        _testDirectory = Path.Combine(Path.GetTempPath(), "LTM_IntegrationTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Clean up test directory
        if (_testDirectory != null && Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    private IServiceProvider CreateServiceProvider(string storageDirectory)
    {
        var services = new ServiceCollection();

        // Register configuration
        var config = new LongTermMemoryConfiguration
        {
            Enabled = true,
            MaxCharacters = 10000,
            AutoLoadOnStart = true,
            PromptUpdateOnEnd = true,
            UpdatePromptTimeoutSeconds = 30
        };
        services.AddSingleton(config);

        // Register mock DataPathService
        var mockDataPathService = new Mock<IDataPathService>();
        mockDataPathService.Setup(x => x.GetMemoryDirectory()).Returns(storageDirectory);
        services.AddSingleton(mockDataPathService.Object);

        // Register mock loggers
        services.AddSingleton(Mock.Of<ILogger<LongTermMemoryService>>());
        services.AddSingleton(Mock.Of<ILogger<LongTermMemoryToolExecutor>>());

        // Register memory service
        services.AddSingleton<ILongTermMemoryService, LongTermMemoryService>();

        // Register tool executor
        services.AddSingleton<LongTermMemoryToolExecutor>();

        // Register tool registry
        services.AddSingleton<BuiltInLongTermMemoryToolRegistry>();

        // Register mock app mode service
        var mockAppModeService = new MockAppModeService();
        services.AddSingleton<IAppModeService>(mockAppModeService);

        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task EndToEnd_EnableMemory_UpdateAndReload_Success()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var memoryService = provider.GetRequiredService<ILongTermMemoryService>();

        // Act - Write memory
        var updateResult = await memoryService.UpdateMemoryAsync(AppMode.Normal, "# Test Memory\nUser preferences stored here");

        // Act - Read memory back
        var retrieved = await memoryService.ReadMemoryAsync(AppMode.Normal);

        // Assert
        Assert.IsTrue(updateResult.Success, "Memory update should succeed");
        Assert.AreEqual("# Test Memory\nUser preferences stored here", retrieved, "Memory content should match");
    }

    [TestMethod]
    public async Task Integration_ModeIsolation_SeparateMemoryFiles()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var memoryService = provider.GetRequiredService<ILongTermMemoryService>();

        // Act - Write different content to each mode
        await memoryService.UpdateMemoryAsync(AppMode.Normal, "Normal mode memory");
        await memoryService.UpdateMemoryAsync(AppMode.Teaching, "Teaching mode memory");

        var normalMemory = await memoryService.ReadMemoryAsync(AppMode.Normal);
        var teachingMemory = await memoryService.ReadMemoryAsync(AppMode.Teaching);

        // Assert
        Assert.AreEqual("Normal mode memory", normalMemory, "Normal mode memory should be isolated");
        Assert.AreEqual("Teaching mode memory", teachingMemory, "Teaching mode memory should be isolated");
        Assert.AreNotEqual(normalMemory, teachingMemory, "Modes should have separate memory");
    }

    [TestMethod]
    public async Task Integration_ToolExecutor_ReadTool_ReturnsMemoryContent()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var memoryService = provider.GetRequiredService<ILongTermMemoryService>();
        var toolExecutor = provider.GetRequiredService<LongTermMemoryToolExecutor>();
        var toolRegistry = provider.GetRequiredService<BuiltInLongTermMemoryToolRegistry>();

        // Setup: Write some memory first
        await memoryService.UpdateMemoryAsync(AppMode.Normal, "Test memory content");

        // Act - Execute read tool
        var readTool = toolRegistry.GetTool("long_term_memory_read");
        Assert.IsNotNull(readTool, "Read tool should exist");

        var result = await toolExecutor.ExecuteAsync(readTool, "{}", CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess, "Tool execution should succeed");
        Assert.AreEqual("Test memory content", result.Content, "Tool should return memory content");
    }

    [TestMethod]
    public async Task Integration_ToolExecutor_UpdateTool_UpdatesMemory()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var memoryService = provider.GetRequiredService<ILongTermMemoryService>();
        var toolExecutor = provider.GetRequiredService<LongTermMemoryToolExecutor>();
        var toolRegistry = provider.GetRequiredService<BuiltInLongTermMemoryToolRegistry>();

        // Act - Execute update tool
        var updateTool = toolRegistry.GetTool("long_term_memory_update");
        Assert.IsNotNull(updateTool, "Update tool should exist");

        var args = @"{""content"": ""Updated via tool"", ""reason"": ""Integration test""}";
        var result = await toolExecutor.ExecuteAsync(updateTool, args, CancellationToken.None);

        // Assert - Verify tool execution succeeded
        Assert.IsTrue(result.IsSuccess, "Tool execution should succeed");

        // Assert - Verify memory was actually updated
        var readBack = await memoryService.ReadMemoryAsync(AppMode.Normal);
        Assert.AreEqual("Updated via tool", readBack, "Memory should be updated via tool");
    }

    [TestMethod]
    public async Task Integration_ToolRegistry_ReturnsAllTools()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var toolRegistry = provider.GetRequiredService<BuiltInLongTermMemoryToolRegistry>();

        // Act
        var tools = toolRegistry.GetAllTools();

        // Assert
        Assert.AreEqual(2, tools.Count, "Should have exactly 2 tools");
        Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_read"), "Should include read tool");
        Assert.IsTrue(tools.Any(t => t.Name == "long_term_memory_update"), "Should include update tool");
    }

    [TestMethod]
    public async Task Integration_SizeLimit_EnforcedAcrossLayers()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var toolExecutor = provider.GetRequiredService<LongTermMemoryToolExecutor>();
        var toolRegistry = provider.GetRequiredService<BuiltInLongTermMemoryToolRegistry>();

        // Act - Try to update with oversized content via tool
        var updateTool = toolRegistry.GetTool("long_term_memory_update");
        Assert.IsNotNull(updateTool);

        var oversizedContent = new string('x', 10001); // Exceeds 10,000 limit
        var args = $@"{{""content"": ""{oversizedContent}"", ""reason"": ""Test""}}";
        var result = await toolExecutor.ExecuteAsync(updateTool, args, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Tool execution should fail for oversized content");
        Assert.IsTrue(result.Content.Contains("exceeds maximum"), "Error message should mention size limit");
    }

    [TestMethod]
    public async Task Integration_FileSystem_PersistsAcrossServiceInstances()
    {
        // Arrange - Create first service instance and write memory
        var provider1 = CreateServiceProvider(_testDirectory!);
        var memoryService1 = provider1.GetRequiredService<ILongTermMemoryService>();
        await memoryService1.UpdateMemoryAsync(AppMode.Normal, "Persisted memory");

        // Act - Create NEW service instance and read memory
        var provider2 = CreateServiceProvider(_testDirectory!);
        var memoryService2 = provider2.GetRequiredService<ILongTermMemoryService>();
        var retrieved = await memoryService2.ReadMemoryAsync(AppMode.Normal);

        // Assert
        Assert.AreEqual("Persisted memory", retrieved, "Memory should persist across service instances");
    }

    [TestMethod]
    public async Task Integration_HasMemory_WorksCorrectly()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var memoryService = provider.GetRequiredService<ILongTermMemoryService>();

        // Act & Assert - Before writing memory
        var hasMemoryBefore = await memoryService.HasMemoryAsync(AppMode.Normal);
        Assert.IsFalse(hasMemoryBefore, "Should not have memory initially");

        // Act - Write memory
        await memoryService.UpdateMemoryAsync(AppMode.Normal, "Some content");

        // Act & Assert - After writing memory
        var hasMemoryAfter = await memoryService.HasMemoryAsync(AppMode.Normal);
        Assert.IsTrue(hasMemoryAfter, "Should have memory after writing");
    }

    [TestMethod]
    public async Task Integration_GetLastUpdateTime_WorksCorrectly()
    {
        // Arrange
        var provider = CreateServiceProvider(_testDirectory!);
        var memoryService = provider.GetRequiredService<ILongTermMemoryService>();

        // Act & Assert - Before writing memory
        var timestampBefore = await memoryService.GetLastUpdateTimeAsync(AppMode.Normal);
        Assert.IsNull(timestampBefore, "Should have no timestamp initially");

        // Act - Write memory
        var beforeWrite = DateTime.UtcNow.AddSeconds(-1); // Add 1 second tolerance for file system timing
        await memoryService.UpdateMemoryAsync(AppMode.Normal, "Some content");
        var afterWrite = DateTime.UtcNow.AddSeconds(1); // Add 1 second tolerance for file system timing

        // Act & Assert - After writing memory
        var timestampAfter = await memoryService.GetLastUpdateTimeAsync(AppMode.Normal);
        Assert.IsNotNull(timestampAfter, "Should have timestamp after writing");
        Assert.IsTrue(timestampAfter >= beforeWrite && timestampAfter <= afterWrite,
            $"Timestamp should be within write window. Expected: {beforeWrite:O} to {afterWrite:O}, Actual: {timestampAfter:O}");
    }
}

/// <summary>
/// Mock implementation of IAppModeService for testing
/// </summary>
internal class MockAppModeService : IAppModeService
{
    public AppMode CurrentMode { get; private set; } = AppMode.Normal;

    public event EventHandler<AppMode>? ModeChanged;

    public Task SwitchModeAsync(AppMode newMode, bool clearConversation = false)
    {
        var oldMode = CurrentMode;
        CurrentMode = newMode;
        if (oldMode != newMode)
        {
            ModeChanged?.Invoke(this, newMode);
        }
        return Task.CompletedTask;
    }
}
