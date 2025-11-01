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
    public void AppendAndGetRenderable_IncompleteTable_BuffersUntilRowComplete()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert - Incomplete table row should be buffered
        var result1 = buffer.AppendAndGetRenderable("| Name ");
        Assert.IsNull(result1, "Incomplete table row should be buffered");

        var result2 = buffer.AppendAndGetRenderable("| Age ");
        Assert.IsNull(result2, "Still incomplete table row should be buffered");

        var result3 = buffer.AppendAndGetRenderable("|\n");
        Assert.IsNotNull(result3, "Complete table row should be flushed");
        Assert.AreEqual("| Name | Age |\n", result3);
    }

    [TestMethod]
    public void AppendAndGetRenderable_IncompleteHeading_BuffersUntilLineEnd()
    {
        // Arrange
        var buffer = new MarkdownStreamingBuffer();

        // Act & Assert
        var result1 = buffer.AppendAndGetRenderable("### Hea");
        Assert.IsNull(result1, "Incomplete heading should be buffered");

        var result2 = buffer.AppendAndGetRenderable("ding Title\n");
        Assert.IsNotNull(result2, "Complete heading should be flushed");
        Assert.AreEqual("### Heading Title\n", result2);
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
}
