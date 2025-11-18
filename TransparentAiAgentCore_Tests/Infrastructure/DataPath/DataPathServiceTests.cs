using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Infrastructure.DataPath;

namespace TransparentAiAgentCore_Tests.Infrastructure.DataPath;

[TestClass]
public class DataPathServiceTests
{
    [TestMethod]
    public void GetUserDataRoot_ReturnsApplicationDataPath()
    {
        // Arrange
        var service = new DataPathService();
        var expectedAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var expectedRoot = Path.Combine(expectedAppData, "TransparentAiAgent");

        // Act
        var result = service.GetUserDataRoot();

        // Assert
        Assert.AreEqual(expectedRoot, result);
    }

    [TestMethod]
    public void GetMemoryDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var service = new DataPathService();
        var expectedAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var expectedPath = Path.Combine(expectedAppData, "TransparentAiAgent", "memory");

        // Act
        var result = service.GetMemoryDirectory();

        // Assert
        Assert.AreEqual(expectedPath, result);
    }

    [TestMethod]
    public void GetConversationsDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var service = new DataPathService();
        var expectedAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var expectedPath = Path.Combine(expectedAppData, "TransparentAiAgent", "conversations");

        // Act
        var result = service.GetConversationsDirectory();

        // Assert
        Assert.AreEqual(expectedPath, result);
    }

    [TestMethod]
    public void GetLogsDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var service = new DataPathService();
        var expectedAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var expectedPath = Path.Combine(expectedAppData, "TransparentAiAgent", "logs");

        // Act
        var result = service.GetLogsDirectory();

        // Assert
        Assert.AreEqual(expectedPath, result);
    }

    [TestMethod]
    public void EnsureDirectoriesExist_CreatesAllDirectories()
    {
        // Arrange
        var service = new DataPathService();
        var memoryDir = service.GetMemoryDirectory();
        var conversationsDir = service.GetConversationsDirectory();
        var logsDir = service.GetLogsDirectory();

        // Clean up first if directories exist from previous test runs
        if (Directory.Exists(memoryDir))
            Directory.Delete(memoryDir, true);
        if (Directory.Exists(conversationsDir))
            Directory.Delete(conversationsDir, true);
        if (Directory.Exists(logsDir))
            Directory.Delete(logsDir, true);

        // Act
        service.EnsureDirectoriesExist();

        // Assert
        Assert.IsTrue(Directory.Exists(memoryDir), "Memory directory was not created");
        Assert.IsTrue(Directory.Exists(conversationsDir), "Conversations directory was not created");
        Assert.IsTrue(Directory.Exists(logsDir), "Logs directory was not created");

        // Cleanup
        Directory.Delete(memoryDir, true);
        Directory.Delete(conversationsDir, true);
        Directory.Delete(logsDir, true);
    }

    [TestMethod]
    public void EnsureDirectoriesExist_DirectoriesAlreadyExist_DoesNotThrow()
    {
        // Arrange
        var service = new DataPathService();
        service.EnsureDirectoriesExist();

        // Act & Assert - should not throw
        service.EnsureDirectoriesExist();

        // Cleanup
        var memoryDir = service.GetMemoryDirectory();
        var conversationsDir = service.GetConversationsDirectory();
        var logsDir = service.GetLogsDirectory();
        if (Directory.Exists(memoryDir))
            Directory.Delete(memoryDir, true);
        if (Directory.Exists(conversationsDir))
            Directory.Delete(conversationsDir, true);
        if (Directory.Exists(logsDir))
            Directory.Delete(logsDir, true);
    }
}
