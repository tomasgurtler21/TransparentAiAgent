# Phase 5 Manual Testing Guide

This guide provides instructions for manually testing the MCP tool integration with real MCP servers.

## Prerequisites

- .NET 8.0 SDK
- Node.js and npx (for JavaScript-based MCP servers)
- Azure OpenAI or Anthropic API credentials configured

## Test Scenario 1: Tool Discovery

**Objective**: Verify that the system can discover tools from a real MCP server.

### Steps:

1. **Configure an MCP Server** in `appsettings.json`:
   ```json
   "MCP": {
     "AutoDiscoverTools": true,
     "ToolExecutionTimeoutSeconds": 180,
     "MaxToolCallDepth": 10,
     "Servers": [
       {
         "Name": "your-server-name",
         "Command": "npx",
         "Args": ["-y", "@your-org/your-mcp-server"],
         "Env": {}
       }
     ]
   }
   ```

2. **Run the Application**:
   ```bash
   cd TransparentAiAgentGui
   dotnet run
   ```

3. **Check Console Output** for tool discovery messages:
   - Look for: `✓ Tool system enabled with X MCP server(s)`
   - Look for: `✓ Discovered X tools from Y MCP server(s)`

4. **Expected Results**:
   - Application starts without errors
   - Console shows successful tool discovery
   - Number of discovered tools matches expectations

## Test Scenario 2: Tool Execution (Simple)

**Objective**: Execute a simple tool call and verify the result.

### Steps:

1. **Start the Application** (with MCP server configured as above)

2. **Send a User Message** that would trigger a tool call:
   - Example: "List all the todos" (if using a todo MCP server)
   - Example: "What's the weather in London?" (if using a weather MCP server)

3. **Observe Conversation History**:
   - Check for `ToolCallMessage` in the conversation
   - Check for `ToolResultMessage` with the result
   - Check for final `AssistantMessage` synthesizing the result

4. **Expected Results**:
   - LLM decides to call a tool
   - Tool executes successfully
   - Result is returned to LLM
   - LLM provides natural language response

## Test Scenario 3: Multi-Turn Tool Calling

**Objective**: Verify that the system can handle multiple tool calls in sequence.

### Steps:

1. **Start the Application**

2. **Send a Complex User Message** requiring multiple tool calls:
   - Example: "Create a todo 'Buy milk', then list all todos"

3. **Monitor Transparency Events**:
   - Check for multiple `ToolCallStarted` events
   - Check for multiple `ToolCallCompleted` events
   - Verify tool call depth doesn't exceed `MaxToolCallDepth`

4. **Expected Results**:
   - Multiple tools are called in sequence
   - Each tool result is fed back to the LLM
   - Final response synthesizes all tool results

## Test Scenario 4: Tool Execution Timeout

**Objective**: Verify that tools respect the execution timeout.

### Steps:

1. **Configure a Short Timeout** in `appsettings.json`:
   ```json
   "ToolExecutionTimeoutSeconds": 5
   ```

2. **Trigger a Long-Running Tool** (if available)

3. **Expected Results**:
   - Tool execution is cancelled after 5 seconds
   - `ToolCallTimeout` transparency event is logged
   - System continues gracefully with timeout error

## Test Scenario 5: Tool Call Depth Limit

**Objective**: Verify that infinite loops are prevented.

### Steps:

1. **Configure a Low Depth Limit** in `appsettings.json`:
   ```json
   "MaxToolCallDepth": 3
   ```

2. **Trigger Multiple Tool Calls** (e.g., complex query requiring many tools)

3. **Expected Results**:
   - After 3 tool call iterations, system stops
   - User receives message: "I've reached the maximum number of tool calls"
   - No infinite loops

## Test Scenario 6: MCP Server Connection Failure

**Objective**: Verify graceful handling of connection failures.

### Steps:

1. **Configure an Invalid MCP Server**:
   ```json
   {
     "Name": "invalid-server",
     "Command": "npx",
     "Args": ["-y", "@nonexistent/mcp-server"],
     "Env": {}
   }
   ```

2. **Run the Application**

3. **Expected Results**:
   - Application starts (doesn't crash)
   - Console shows: `⚠ Tool discovery failed: <error details>`
   - `MCPServerConnectionFailed` transparency event logged
   - User can still interact with assistant (without tools)

## Test Scenario 7: Tool Execution Error

**Objective**: Verify graceful handling of tool execution failures.

### Steps:

1. **Trigger a Tool with Invalid Arguments**
   - Example: "Add a todo with no task name" (should fail validation)

2. **Expected Results**:
   - Tool execution fails gracefully
   - `ToolCallFailed` transparency event logged
   - LLM receives error message
   - LLM can respond appropriately to the error

## Integration Test Checklist

Use this checklist for comprehensive end-to-end testing:

- [ ] Tool discovery from real MCP server succeeds
- [ ] Tools appear in LLM request with correct schema
- [ ] LLM decides to call a tool when appropriate
- [ ] Tool execution succeeds with valid arguments
- [ ] Tool result is returned to LLM
- [ ] LLM synthesizes natural language response
- [ ] Multiple tool calls work in sequence
- [ ] Tool execution timeout is respected
- [ ] Max tool call depth is enforced
- [ ] Connection failures are handled gracefully
- [ ] Tool execution errors are handled gracefully
- [ ] All transparency events are logged correctly
- [ ] Conversation history is maintained correctly
- [ ] UI displays tool calls and results (Phase 5 Step 14)

## Available MCP Servers for Testing

### Option 1: Create a Simple Test Server

Create a minimal MCP server for testing:

```javascript
// test-mcp-server.js
const { MCPServer } = require('@modelcontextprotocol/sdk');

const server = new MCPServer({
  name: 'test-server',
  version: '1.0.0'
});

server.registerTool({
  name: 'echo',
  description: 'Echoes back the input',
  schema: {
    type: 'object',
    properties: {
      message: { type: 'string', description: 'Message to echo' }
    },
    required: ['message']
  },
  handler: async (params) => {
    return { content: [{ type: 'text', text: `Echo: ${params.message}` }] };
  }
});

server.start();
```

Run with: `node test-mcp-server.js`

### Option 2: Use Public MCP Servers

Check the Model Context Protocol GitHub organization for available servers:
- https://github.com/modelcontextprotocol

### Option 3: Use Python-based MCP Servers

The MCP SDK is also available for Python. Many reference implementations exist.

## Debugging Tips

1. **Enable Verbose Logging**: Check `appsettings.json` logging configuration
2. **Check Transparency Events**: All tool operations are logged
3. **Monitor Console Output**: Tool discovery and execution are logged to console
4. **Use Breakpoints**: Debug tool execution in MCPToolExecutor.cs
5. **Check MCP Server Logs**: Many MCP servers write logs to stderr

## Known Limitations

1. **Tool Schema Extraction**: Currently returns empty schema (`{}`). Needs enhancement.
2. **ContentBlock Parsing**: Currently serializes entire result to JSON. Needs refinement.
3. **Parallel Tool Execution**: Currently sequential only. Parallel mode designed but not implemented.

## Next Steps

After successful manual testing:

1. Document any issues found
2. Add more comprehensive error handling (Step 13)
3. Implement UI updates for tool display (Step 14)
4. Consider adding more built-in tools beyond MCP
