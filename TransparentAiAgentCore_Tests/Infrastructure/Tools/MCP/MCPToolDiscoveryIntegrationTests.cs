using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.MCP;

namespace TransparentAiAgentCore_Tests.Infrastructure.Tools.MCP;

[TestClass]
public class MCPToolDiscoveryIntegrationTests
{
    /// <summary>
    /// Integration test: Discovers tools from real MCP server.
    /// NOTE: This test is marked as Ignore because it requires a real MCP server.
    /// To run this test manually:
    /// 1. Install a real MCP server (e.g., from GitHub)
    /// 2. Update the Command and Args below
    /// 3. Remove the [Ignore] attribute
    /// 4. Run the test
    /// </summary>
    [TestMethod]
    [Ignore("Requires real MCP server - see test documentation")]
    public async Task DiscoverToolsFromRealTodoListServer_ShouldSucceed()
    {
        // Arrange
        var toolsConfig = new ToolsConfiguration
        {
            Servers = new List<MCPServerConfiguration>
            {
                new MCPServerConfiguration
                {
                    Name = "todo-list",
                    Command = "npx",
                    Args = new List<string> { "-y", "@anthropic/mcp-server-todo-list" },
                    Env = new Dictionary<string, string>()
                }
            }
        };

        var discovery = new MCPToolDiscovery(toolsConfig);

        // Act
        var tools = await discovery.DiscoverAllToolsAsync(CancellationToken.None);

        // Assert
        Assert.IsNotNull(tools);
        Assert.IsTrue(tools.Count > 0, "Should discover at least one tool from todo-list server");

        // Verify tool properties
        var firstTool = tools[0];
        Assert.IsNotNull(firstTool.Name);
        Assert.IsNotNull(firstTool.Description);
        Assert.AreEqual(ToolSourceType.MCP, firstTool.SourceType);

        // Print discovered tools for manual verification
        Console.WriteLine($"Discovered {tools.Count} tools:");
        foreach (var tool in tools)
        {
            Console.WriteLine($"  - {tool.Name}: {tool.Description}");
        }
    }

    /// <summary>
    /// Integration test: Executes a real tool call to create a todo item.
    /// NOTE: This test is marked as Ignore because it requires a real MCP server.
    /// To run this test manually:
    /// 1. Install a real MCP server (e.g., from GitHub)
    /// 2. Update the Command and Args below
    /// 3. Remove the [Ignore] attribute
    /// 4. Run the test
    /// </summary>
    [TestMethod]
    [Ignore("Requires real MCP server - see test documentation")]
    public async Task ExecuteToolCall_CreateTodoItem_ShouldSucceed()
    {
        // Arrange
        var toolsConfig = new ToolsConfiguration
        {
            Servers = new List<MCPServerConfiguration>
            {
                new MCPServerConfiguration
                {
                    Name = "todo-list",
                    Command = "npx",
                    Args = new List<string> { "-y", "@anthropic/mcp-server-todo-list" },
                    Env = new Dictionary<string, string>()
                }
            }
        };

        var discovery = new MCPToolDiscovery(toolsConfig);
        var executor = new MCPToolExecutor(discovery);

        // Discover tools first
        var tools = await discovery.DiscoverAllToolsAsync(CancellationToken.None);
        Assert.IsTrue(tools.Count > 0, "Should discover tools");

        // Find the add_todo tool (or similar)
        var addTodoTool = tools.FirstOrDefault(t =>
            t.Name.Contains("add", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("create", StringComparison.OrdinalIgnoreCase));

        if (addTodoTool == null)
        {
            Assert.Inconclusive("Could not find add/create todo tool. Available tools: " +
                string.Join(", ", tools.Select(t => t.Name)));
            return;
        }

        Console.WriteLine($"Using tool: {addTodoTool.Name}");

        // Act - Execute tool call
        var arguments = "{\"task\": \"Test todo from integration test\"}";
        var result = await executor.ExecuteAsync(addTodoTool, arguments, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess, $"Tool execution should succeed. Error: {result.ErrorMessage}");
        Assert.IsNotNull(result.Content);
        Console.WriteLine($"Tool result: {result.Content}");
        Console.WriteLine($"Execution time: {result.ExecutionTime.TotalMilliseconds}ms");
    }
}
