namespace TransparentAiAgentCore_Tests.Domain.Knowledge;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Domain.Knowledge;

[TestClass]
public class KnowledgeExampleTests
{
    [TestMethod]
    public void Constructor_NullTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeExample(null!, "explanation"));
    }

    [TestMethod]
    public void Constructor_EmptyTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeExample("", "explanation"));
    }

    [TestMethod]
    public void Constructor_WhitespaceTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeExample("   ", "explanation"));
    }

    [TestMethod]
    public void Constructor_NullExplanation_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeExample("title", null!));
    }

    [TestMethod]
    public void Constructor_EmptyExplanation_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new KnowledgeExample("title", ""));
    }

    [TestMethod]
    public void Constructor_ValidTitle_CreatesInstance()
    {
        // Arrange
        var title = "Example Title";
        var explanation = "Example Explanation";

        // Act
        var example = new KnowledgeExample(title, explanation);

        // Assert
        Assert.IsNotNull(example);
        Assert.AreEqual(title, example.Title);
        Assert.AreEqual(explanation, example.Explanation);
    }

    [TestMethod]
    public void Constructor_NullCode_SetsEmptyString()
    {
        // Arrange
        var title = "Example Title";
        var explanation = "Example Explanation";

        // Act
        var example = new KnowledgeExample(title, explanation, code: null);

        // Assert
        Assert.AreEqual(string.Empty, example.Code);
    }

    [TestMethod]
    public void Constructor_ValidCode_StoresCode()
    {
        // Arrange
        var title = "Example Title";
        var explanation = "Example Explanation";
        var code = "var x = 1;";

        // Act
        var example = new KnowledgeExample(title, explanation, code);

        // Assert
        Assert.AreEqual(code, example.Code);
    }
}
