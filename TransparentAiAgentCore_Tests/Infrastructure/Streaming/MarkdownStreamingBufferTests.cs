using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransparentAiAgentCore.Infrastructure.Streaming;

namespace TransparentAiAgentCore_Tests.Infrastructure.Streaming;

[TestClass]
public class MarkdownStreamingBufferTests
{
    [TestMethod]
    public void AppendAndGetRenderable_PlainText_ReturnsImmediately()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act
        var result = buffer.AppendAndGetRenderable("Hello ");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello ", result);
    }

    [TestMethod]
    public void AppendAndGetRenderable_IncompleteCodeBlock_BuffersUntilComplete()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert - Opening fence should be buffered
        var result1 = buffer.AppendAndGetRenderable("```python");
        Assert.IsNull(result1, "Opening code fence should be buffered");

        var result2 = buffer.AppendAndGetRenderable("\n");
        Assert.IsNull(result2, "Newline after fence should be buffered");

        var result3 = buffer.AppendAndGetRenderable("print('hello')");
        Assert.IsNull(result3, "Code content should be buffered");

        var result4 = buffer.AppendAndGetRenderable("\n```");
        Assert.IsNotNull(result4, "Complete code block should be flushed");
        Assert.IsTrue(result4.Contains("```python"), "Should contain opening fence");
        Assert.IsTrue(result4.Contains("print('hello')"), "Should contain code");
        Assert.IsTrue(result4.Contains("```"), "Should contain closing fence");
    }

    [TestMethod]
    public void AppendAndGetRenderable_IncompleteTable_FlushesImmediately()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert - Incomplete table should flush immediately (browser handles gracefully)
        var result1 = buffer.AppendAndGetRenderable("| Name ");
        Assert.IsNotNull(result1, "Incomplete table should flush immediately");
        Assert.AreEqual("| Name ", result1);

        var result2 = buffer.AppendAndGetRenderable("| Age ");
        Assert.IsNotNull(result2, "Table continuation should flush immediately");
        Assert.AreEqual("| Age ", result2);

        var result3 = buffer.AppendAndGetRenderable("|\n");
        Assert.IsNotNull(result3, "Table row end should flush immediately");
        Assert.AreEqual("|\n", result3);
    }

    [TestMethod]
    public void AppendAndGetRenderable_IncompleteHeading_FlushesImmediately()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert - Incomplete heading should flush immediately (browser handles gracefully)
        var result1 = buffer.AppendAndGetRenderable("### Hea");
        Assert.IsNotNull(result1, "Incomplete heading should flush immediately");
        Assert.AreEqual("### Hea", result1);

        var result2 = buffer.AppendAndGetRenderable("ding Title\n");
        Assert.IsNotNull(result2, "Heading continuation should flush immediately");
        Assert.AreEqual("ding Title\n", result2);
    }

    [TestMethod]
    public void AppendAndGetRenderable_MixedContent_CorrectlyBuffersAndFlushes()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert - Plain text followed by code block
        var result1 = buffer.AppendAndGetRenderable("Here is some code:\n");
        Assert.IsNotNull(result1);
        Assert.AreEqual("Here is some code:\n", result1);

        var result2 = buffer.AppendAndGetRenderable("```js\n");
        Assert.IsNull(result2, "Opening code fence should be buffered");

        var result3 = buffer.AppendAndGetRenderable("console.log('test');\n");
        Assert.IsNull(result3, "Code content should be buffered");

        var result4 = buffer.AppendAndGetRenderable("```\n");
        Assert.IsNotNull(result4, "Complete code block should be flushed");
    }

    [TestMethod]
    public void Flush_BufferedContent_ReturnsAllContent()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();
        buffer.AppendAndGetRenderable("```python");
        buffer.AppendAndGetRenderable("\ncode");

        // Act
        var result = buffer.Flush();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("```python\ncode", result);
    }

    [TestMethod]
    public void AppendAndGetRenderable_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act
        var result = buffer.AppendAndGetRenderable("");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void AppendAndGetRenderable_MultipleCodeBlocks_CorrectlyIdentifies()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert - Complete code block should flush
        var result1 = buffer.AppendAndGetRenderable("```js\n");
        Assert.IsNull(result1, "Opening fence should buffer");

        var result2 = buffer.AppendAndGetRenderable("code\n```\n");
        Assert.IsNotNull(result2, "Complete code block should flush");
        Assert.AreEqual("```js\ncode\n```\n", result2);

        // Start of second code block should buffer
        var result3 = buffer.AppendAndGetRenderable("```python\n");
        Assert.IsNull(result3, "Second opening fence should buffer");

        var result4 = buffer.AppendAndGetRenderable("print('test')\n```\n");
        Assert.IsNotNull(result4, "Second complete code block should flush");
        Assert.AreEqual("```python\nprint('test')\n```\n", result4);
    }

    [TestMethod]
    public void AppendAndGetRenderable_InlineCode_FlushesImmediately()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act - Inline code with backticks should NOT be buffered
        var result = buffer.AppendAndGetRenderable("Use `console.log()` for debugging");

        // Assert
        Assert.IsNotNull(result, "Inline code should flush immediately");
        Assert.AreEqual("Use `console.log()` for debugging", result);
    }

    [TestMethod]
    public void AppendAndGetRenderable_CodeFenceWithLanguage_BuffersCorrectly()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert
        var result1 = buffer.AppendAndGetRenderable("```typescript\n");
        Assert.IsNull(result1, "Code fence with language should buffer");

        var result2 = buffer.AppendAndGetRenderable("const x = 5;\n");
        Assert.IsNull(result2, "Code content should buffer");

        var result3 = buffer.AppendAndGetRenderable("```\n");
        Assert.IsNotNull(result3, "Complete code block should flush");
        Assert.AreEqual("```typescript\nconst x = 5;\n```\n", result3);
    }

    [TestMethod]
    public void AppendAndGetRenderable_CodeFenceVariations_BuffersCorrectly()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert - Test different language identifiers
        var result1 = buffer.AppendAndGetRenderable("```csharp\n");
        Assert.IsNull(result1, "CSharp fence should buffer");

        var result2 = buffer.AppendAndGetRenderable("var x = 5;\n```\n");
        Assert.IsNotNull(result2, "Complete block should flush");

        // Test fence without language
        var result3 = buffer.AppendAndGetRenderable("```\n");
        Assert.IsNull(result3, "Plain fence should buffer");

        var result4 = buffer.AppendAndGetRenderable("plain code\n```\n");
        Assert.IsNotNull(result4, "Complete plain block should flush");
    }
}
