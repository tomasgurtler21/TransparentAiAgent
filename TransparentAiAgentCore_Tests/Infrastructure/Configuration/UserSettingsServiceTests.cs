using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Infrastructure.Configuration;
using TransparentAiAgentCore.Infrastructure.DataPath;

namespace TransparentAiAgentCore_Tests.Infrastructure.Configuration;

[TestClass]
public class UserSettingsServiceTests
{
    private string _testDirectory = string.Empty;
    private Mock<IDataPathService> _mockDataPathService = new();
    private Mock<ILogger<UserSettingsService>> _mockLogger = new();

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"UserSettingsTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _mockDataPathService = new Mock<IDataPathService>();
        _mockDataPathService.Setup(x => x.GetUserDataRoot()).Returns(_testDirectory);
        _mockLogger = new Mock<ILogger<UserSettingsService>>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [TestMethod]
    public void LoadSettings_FileDoesNotExist_ReturnsDefaults()
    {
        // Arrange
        var service = new UserSettingsService(_mockDataPathService.Object, _mockLogger.Object);

        // Act
        var settings = service.LoadSettings();

        // Assert
        Assert.IsNotNull(settings);
        Assert.AreEqual(200, settings.ContextWindowSize); // Default value
        Assert.IsFalse(settings.EnableMemory); // Default value
    }

    [TestMethod]
    public void SaveSettings_CreatesFile()
    {
        // Arrange
        var service = new UserSettingsService(_mockDataPathService.Object, _mockLogger.Object);
        var settings = new UserSettings
        {
            ContextWindowSize = 300,
            EnableMemory = true
        };

        // Act
        service.SaveSettings(settings);

        // Assert
        var filePath = Path.Combine(_testDirectory, "user-settings.json");
        Assert.IsTrue(File.Exists(filePath));
    }

    [TestMethod]
    public void SaveAndLoad_PreservesSettings()
    {
        // Arrange
        var service = new UserSettingsService(_mockDataPathService.Object, _mockLogger.Object);
        var originalSettings = new UserSettings
        {
            ContextWindowSize = 500,
            EnableMemory = true
        };

        // Act
        service.SaveSettings(originalSettings);
        var loadedSettings = service.LoadSettings();

        // Assert
        Assert.AreEqual(500, loadedSettings.ContextWindowSize);
        Assert.IsTrue(loadedSettings.EnableMemory);
    }

    [TestMethod]
    public void UpdateContextWindowSize_UpdatesSettingAndSaves()
    {
        // Arrange
        var service = new UserSettingsService(_mockDataPathService.Object, _mockLogger.Object);

        // Act
        service.UpdateContextWindowSize(400);
        var settings = service.LoadSettings();

        // Assert
        Assert.AreEqual(400, settings.ContextWindowSize);
    }

    [TestMethod]
    public void UpdateEnableMemory_UpdatesSettingAndSaves()
    {
        // Arrange
        var service = new UserSettingsService(_mockDataPathService.Object, _mockLogger.Object);

        // Act
        service.UpdateEnableMemory(true);
        var settings = service.LoadSettings();

        // Assert
        Assert.IsTrue(settings.EnableMemory);
    }

    [TestMethod]
    public void GetCurrentSettings_ReturnsLoadedSettings()
    {
        // Arrange
        var service = new UserSettingsService(_mockDataPathService.Object, _mockLogger.Object);
        var settings = new UserSettings { ContextWindowSize = 250, EnableMemory = true };
        service.SaveSettings(settings);

        // Act
        var currentSettings = service.GetCurrentSettings();

        // Assert
        Assert.AreEqual(250, currentSettings.ContextWindowSize);
        Assert.IsTrue(currentSettings.EnableMemory);
    }
}
