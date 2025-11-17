using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Application.Localization;

namespace TransparentAiAgentCore_Tests.Application.Localization;

[TestClass]
public class LanguageServiceTests
{
    [TestMethod]
    public void CurrentLanguage_DefaultValue_IsEnglish()
    {
        // Arrange & Act
        var service = new LanguageService();

        // Assert
        Assert.AreEqual("en", service.CurrentLanguage);
    }

    [TestMethod]
    public void SetLanguage_ValidLanguage_UpdatesCurrentLanguage()
    {
        // Arrange
        var service = new LanguageService();

        // Act
        service.SetLanguage("de");

        // Assert
        Assert.AreEqual("de", service.CurrentLanguage);
    }

    [TestMethod]
    public void SetLanguage_DifferentLanguage_RaisesLanguageChangedEvent()
    {
        // Arrange
        var service = new LanguageService();
        string? eventLanguage = null;
        service.LanguageChanged += (sender, lang) => eventLanguage = lang;

        // Act
        service.SetLanguage("de");

        // Assert
        Assert.AreEqual("de", eventLanguage);
    }

    [TestMethod]
    public void SetLanguage_SameLanguage_DoesNotRaiseEvent()
    {
        // Arrange
        var service = new LanguageService();
        service.SetLanguage("de");
        int eventCount = 0;
        service.LanguageChanged += (sender, lang) => eventCount++;

        // Act
        service.SetLanguage("de"); // Same language

        // Assert
        Assert.AreEqual(0, eventCount);
    }

    [TestMethod]
    public void SetLanguage_NullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        var service = new LanguageService();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => service.SetLanguage(null!));
        Assert.ThrowsException<ArgumentException>(() => service.SetLanguage(""));
        Assert.ThrowsException<ArgumentException>(() => service.SetLanguage("   "));
    }
}
