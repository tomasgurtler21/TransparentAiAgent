using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Moq;
using TransparentAiAgentCore.Infrastructure.Memory;
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Domain.UIControl;

namespace TransparentAiAgentCore_Tests.Infrastructure.Memory;

[TestClass]
public class LongTermMemoryServiceTests
{
    private string _tempDirectory = string.Empty;
    private LongTermMemoryConfiguration _config = new();

    [TestInitialize]
    public void Setup()
    {
        // Create unique temp directory for each test
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"MemoryServiceTests_{Guid.NewGuid()}");
        _config = new LongTermMemoryConfiguration
        {
            StorageDirectory = _tempDirectory
        };
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up temp directory after each test
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    private LongTermMemoryService CreateService(LongTermMemoryConfiguration? config = null)
    {
        return new LongTermMemoryService(
            config ?? _config,
            Mock.Of<ILogger<LongTermMemoryService>>());
    }

    // Test 2.1.1: ReadMemoryAsync - File Doesn't Exist
    [TestMethod]
    public async Task ReadMemoryAsync_FileDoesNotExist_ReturnsEmptyString()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ReadMemoryAsync(AppMode.Normal);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    // Test 2.1.2: UpdateMemoryAsync - Creates New File
    [TestMethod]
    public async Task UpdateMemoryAsync_NewFile_CreatesFileWithContent()
    {
        // Arrange
        var service = CreateService();
        var content = "# Test Memory\nContent here";

        // Act
        var result = await service.UpdateMemoryAsync(AppMode.Normal, content);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(content.Length, result.CharacterCount);

        var readBack = await service.ReadMemoryAsync(AppMode.Normal);
        Assert.AreEqual(content, readBack);
    }

    // Test 2.1.3: UpdateMemoryAsync - Overwrites Existing File
    [TestMethod]
    public async Task UpdateMemoryAsync_ExistingFile_OverwritesContent()
    {
        // Arrange
        var service = CreateService();
        await service.UpdateMemoryAsync(AppMode.Normal, "Old content");

        // Act
        var newContent = "New content";
        var result = await service.UpdateMemoryAsync(AppMode.Normal, newContent);

        // Assert
        Assert.IsTrue(result.Success);
        var readBack = await service.ReadMemoryAsync(AppMode.Normal);
        Assert.AreEqual(newContent, readBack);
    }

    // Test 2.1.4: UpdateMemoryAsync - Size Limit Enforcement
    [TestMethod]
    public async Task UpdateMemoryAsync_ContentExceedsMaxSize_ReturnsFailure()
    {
        // Arrange
        var config = new LongTermMemoryConfiguration
        {
            StorageDirectory = _tempDirectory,
            MaxCharacters = 100
        };
        var service = CreateService(config);
        var oversizedContent = new string('x', 101);

        // Act
        var result = await service.UpdateMemoryAsync(AppMode.Normal, oversizedContent);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("exceeds maximum"));
    }

    // Test 2.1.5: Mode-Aware File Selection
    [TestMethod]
    public async Task MemoryService_DifferentModes_UsesSeparateFiles()
    {
        // Arrange
        var service = CreateService();
        var normalContent = "Normal mode memory";
        var teachingContent = "Teaching mode memory";

        // Act
        await service.UpdateMemoryAsync(AppMode.Normal, normalContent);
        await service.UpdateMemoryAsync(AppMode.Teaching, teachingContent);

        var normalRead = await service.ReadMemoryAsync(AppMode.Normal);
        var teachingRead = await service.ReadMemoryAsync(AppMode.Teaching);

        // Assert
        Assert.AreEqual(normalContent, normalRead);
        Assert.AreEqual(teachingContent, teachingRead);
        Assert.AreNotEqual(normalRead, teachingRead);
    }

    // Test 2.1.6: HasMemoryAsync
    [TestMethod]
    public async Task HasMemoryAsync_FileExists_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        await service.UpdateMemoryAsync(AppMode.Normal, "Content");

        // Act
        var result = await service.HasMemoryAsync(AppMode.Normal);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HasMemoryAsync_FileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.HasMemoryAsync(AppMode.Normal);

        // Assert
        Assert.IsFalse(result);
    }

    // Test 2.1.7: GetLastUpdateTimeAsync
    [TestMethod]
    public async Task GetLastUpdateTimeAsync_FileExists_ReturnsTimestamp()
    {
        // Arrange
        var service = CreateService();
        var before = DateTime.UtcNow.AddSeconds(-1); // Allow 1 second tolerance for filesystem precision
        await service.UpdateMemoryAsync(AppMode.Normal, "Content");
        var after = DateTime.UtcNow.AddSeconds(1);

        // Act
        var result = await service.GetLastUpdateTimeAsync(AppMode.Normal);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result >= before && result <= after,
            $"Expected timestamp between {before:O} and {after:O}, but got {result:O}");
    }

    [TestMethod]
    public async Task GetLastUpdateTimeAsync_FileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetLastUpdateTimeAsync(AppMode.Normal);

        // Assert
        Assert.IsNull(result);
    }

    // Test 2.1.10: UTF-8 Encoding
    [TestMethod]
    public async Task UpdateMemoryAsync_UnicodeContent_PreservesEncoding()
    {
        // Arrange
        var service = CreateService();
        var content = "# Memory\n- Name: José 👋\n- Emoji: 🚀";

        // Act
        await service.UpdateMemoryAsync(AppMode.Normal, content);
        var readBack = await service.ReadMemoryAsync(AppMode.Normal);

        // Assert
        Assert.AreEqual(content, readBack);
    }

    // Test 2.1.9: File Path Validation (Security)
    [TestMethod]
    public void Constructor_DirectoryTraversalAttempt_ThrowsException()
    {
        // Arrange
        var config = new LongTermMemoryConfiguration
        {
            StorageDirectory = "../../etc/passwd" // Attempt directory traversal
        };

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            CreateService(config));

        Assert.IsTrue(ex.Message.Contains("application directory"));
    }

    // Test 2.1.11: Directory Auto-Creation
    [TestMethod]
    public async Task UpdateMemoryAsync_DirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = new LongTermMemoryConfiguration
        {
            StorageDirectory = tempPath
        };
        var service = CreateService(config);

        // Act
        var result = await service.UpdateMemoryAsync(AppMode.Normal, "test");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(Directory.Exists(tempPath));

        // Cleanup
        Directory.Delete(tempPath, true);
    }
}
