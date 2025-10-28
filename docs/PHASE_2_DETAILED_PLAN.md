# Phase 2: LLM Integration - Detailed Implementation Plan

**Status**: Implementation Ready
**Last Updated**: 2025-10-28
**Dependencies**: Phase 1 must be complete

## Overview

This document provides a comprehensive, class-by-class implementation plan for Phase 2 using Test-Driven Development (TDD) with **Lean TDD** principles.

## Goals

Build the LLM integration layer that enables communication with LLM providers:
- LLM provider abstraction for multi-provider support
- Azure OpenAI provider implementation with streaming
- Authentication management for API access
- Request/response models for LLM communication
- Tool calling support (function calling)
- Comprehensive error handling

## Architecture Layer: Domain + Infrastructure

Phase 2 focuses on:
- **Domain Layer**: LLM interfaces, request/response models, authentication abstractions
- **Infrastructure Layer**: Azure OpenAI provider implementation, authentication providers, streaming handlers

## Provider-Agnostic Design Pattern

**CRITICAL ARCHITECTURAL CONCEPT**: This phase implements a provider-agnostic abstraction layer.

### The Strategy

Our domain models (`LLMMessage`, `LLMRequest`, `LLMResponse`, etc.) represent a **universal format** that is provider-neutral. Each provider implementation handles conversion to/from its specific format.

```
┌─────────────────────────────────────────────┐
│  Agent Core / Application Layer             │
│  Knows ONLY about: LLMRequest, LLMResponse  │
│  (Provider-agnostic domain models)          │
└────────────────┬────────────────────────────┘
                 │ Uses interface only
                 ▼
┌─────────────────────────────────────────────┐
│  ILLMProvider Interface                     │
│  SendRequestAsync(LLMRequest) → LLMResponse │
│  (Uses OUR domain models, not provider SDKs)│
└────────────────┬────────────────────────────┘
                 │ Implemented by
        ┌────────┴────────┐
        ▼                 ▼
┌───────────────┐  ┌──────────────────┐
│ AzureOpenAI   │  │ Anthropic        │
│ Provider      │  │ Provider         │
│               │  │ (Phase 8)        │
└───────────────┘  └──────────────────┘
        │                 │
        ▼                 ▼
  Converts:         Converts:
  LLMMessage        LLMMessage
    ↓ ↑               ↓ ↑
  Azure SDK        Anthropic SDK
  Format           Format
```

### How Conversion Works

**Request Flow (Agent → Provider):**
```csharp
// 1. Agent creates request in OUR format
var request = new LLMRequest(
    messages: List<LLMMessage>,  // Our domain model
    tools: List<LLMTool>         // Our domain model
);

// 2. Send via interface (agent doesn't know which provider)
var response = await provider.SendRequestAsync(request);

// 3. Inside AzureOpenAIProvider:
private ChatCompletionsOptions BuildChatCompletionsOptions(LLMRequest request)
{
    // Convert OUR LLMMessage → Azure ChatRequestMessage
    foreach (var msg in request.Messages)
    {
        azureOptions.Messages.Add(ConvertToAzureMessage(msg));
    }
    // Convert OUR LLMTool → Azure ChatCompletionsFunctionToolDefinition
    foreach (var tool in request.Tools)
    {
        azureOptions.Tools.Add(ConvertToAzureTool(tool));
    }
}

// 4. Inside AnthropicProvider (Phase 8):
private AnthropicRequest BuildAnthropicRequest(LLMRequest request)
{
    // Convert OUR LLMMessage → Anthropic Message format
    foreach (var msg in request.Messages)
    {
        anthropicReq.Messages.Add(ConvertToAnthropicMessage(msg));
    }
    // Handle Anthropic-specific differences (e.g., system prompt as parameter)
}
```

**Response Flow (Provider → Agent):**
```csharp
// 5. Provider receives provider-specific response, converts to OUR format
private LLMResponse ConvertResponse(ChatCompletions azureResponse)
{
    // Azure format → OUR LLMResponse
    return new LLMResponse(
        content: azureResponse.Choices[0].Message.Content,
        toolCalls: ConvertFromAzureToolCalls(...)
    );
}

// 6. Agent receives LLMResponse (same format regardless of provider!)
```

### Provider Differences and How We Handle Them

| Feature | Azure OpenAI | Anthropic Claude | Our Universal Model |
|---------|--------------|------------------|---------------------|
| **Message Roles** | `user`, `assistant`, `system`, `tool` | `user`, `assistant` (system is parameter) | `string Role` (flexible, converts appropriately) |
| **System Prompt** | Regular message with `system` role | Separate `system` parameter | Message with role="system" (each provider converts) |
| **Tool Calls** | `ToolCalls` array in assistant message | `tool_use` content blocks | `List<LLMToolCall>` (maps to both) |
| **Tool Results** | Message with role=`tool` | `tool_result` content blocks | `LLMMessage` with `ToolCallId` (converts to both) |
| **Streaming** | `ContentUpdate` deltas | Event stream with typed blocks | `StreamingLLMChunk` with deltas |

### Benefits of This Approach

✅ **Agent Core is completely provider-agnostic**
   - Never imports Azure or Anthropic SDKs
   - Can switch providers via configuration change only
   - Same code works with any provider

✅ **Easy to add new providers**
   - Implement `ILLMProvider` interface
   - Add conversion methods
   - No changes to Agent Core needed

✅ **Testable**
   - Mock `ILLMProvider` in Agent Core tests
   - Test providers independently with mocked SDKs
   - Test conversion logic separately

✅ **Maintainable**
   - Provider-specific complexity isolated
   - Changes to one provider don't affect others
   - Clear separation of concerns

### Potential Challenges

**Challenge 1: Provider-Specific Features**
- **Problem**: Anthropic has "thinking" process, Azure doesn't
- **Solution**: Add optional fields to our models, populate when available

**Challenge 2: Different Defaults**
- **Problem**: Providers have different default values
- **Solution**: Our domain models define sensible defaults, providers adapt

**Challenge 3: Message Format Incompatibilities**
- **Problem**: Some provider features don't map 1:1
- **Solution**: Provider implementations handle edge cases, potentially log warnings

### Design Pattern Used

This implements the **Adapter Pattern**:
- `ILLMProvider` = Target interface
- `AzureOpenAIProvider` = Adapter for Azure SDK
- `AnthropicProvider` = Adapter for Anthropic SDK
- Provider-specific conversion methods = Adaptation logic

Combined with **Strategy Pattern**:
- `ILLMProvider` = Strategy interface
- Different providers = Concrete strategies
- `LLMProviderFactory` = Strategy selection

---

## Lean TDD Approach (Same as Phase 1)

**IMPORTANT**: We follow **Lean TDD**, not pedantic testing.

### What We DO Test:
✅ **Business Logic & Behavior**
- Validation logic (e.g., "null API key throws exception")
- Request building logic (e.g., "messages are transformed correctly")
- Response parsing (e.g., "streaming chunks are accumulated correctly")
- Authentication behavior (e.g., "credentials are retrieved from config")
- Error handling (e.g., "API errors are wrapped in LLMException")
- State management (e.g., "streaming state is tracked correctly")
- Tool call extraction (e.g., "function calls are parsed from response")

### What We DON'T Test:
❌ **Compiler-Enforced or Trivial Features**
- Simple property getters/setters
- Interface definitions without logic
- HTTP client behavior (framework feature)
- Azure OpenAI API itself (third-party service)

---

## Components & Implementation Order

### 1. Domain Models for LLM (Domain Layer)

#### 1.1 LLM Request/Response Models

##### `LLMMessage` Class
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a message in the format expected by LLM providers
/// </summary>
public class LLMMessage
{
    public string Role { get; }
    public string Content { get; }
    public List<LLMToolCall>? ToolCalls { get; }
    public string? ToolCallId { get; }

    // For regular messages (user, assistant, system)
    public LLMMessage(string role, string content)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Role = role;
        Content = content;
    }

    // For assistant messages with tool calls
    public LLMMessage(string role, string content, List<LLMToolCall> toolCalls)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));

        Role = role;
        Content = content ?? string.Empty; // Content can be empty when there are tool calls
        ToolCalls = toolCalls ?? throw new ArgumentNullException(nameof(toolCalls));
    }

    // For tool result messages
    public LLMMessage(string role, string content, string toolCallId)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));

        Role = role;
        Content = content;
        ToolCallId = toolCallId;
    }
}
```

**Tests** (Lean):
- ✅ Null/empty role throws ArgumentException
- ✅ Null/empty content throws ArgumentException (regular messages)
- ✅ Null toolCalls throws ArgumentNullException (tool call constructor)
- ✅ Null/empty toolCallId throws ArgumentException (tool result constructor)
- ✅ Tool call message allows empty content (valid scenario)

---

##### `LLMToolCall` Class
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a tool call request from the LLM
/// </summary>
public class LLMToolCall
{
    public string Id { get; }
    public string Name { get; }
    public string Arguments { get; } // JSON string

    public LLMToolCall(string id, string name, string arguments)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(name));

        Id = id;
        Name = name;
        Arguments = arguments ?? "{}"; // Default to empty object
    }
}
```

**Tests** (Lean):
- ✅ Null/empty id throws ArgumentException
- ✅ Null/empty name throws ArgumentException
- ✅ Null arguments defaults to "{}"

---

##### `LLMTool` Class
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a tool definition to send to the LLM
/// </summary>
public class LLMTool
{
    public string Name { get; }
    public string Description { get; }
    public string ParametersSchema { get; } // JSON schema

    public LLMTool(string name, string description, string parametersSchema)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Tool description cannot be null or whitespace", nameof(description));

        Name = name;
        Description = description;
        ParametersSchema = parametersSchema ?? "{}";
    }
}
```

**Tests** (Lean):
- ✅ Null/empty name throws ArgumentException
- ✅ Null/empty description throws ArgumentException
- ✅ Null parametersSchema defaults to "{}"

---

##### `LLMRequest` Class
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a request to an LLM provider
/// </summary>
public class LLMRequest
{
    public List<LLMMessage> Messages { get; }
    public List<LLMTool>? Tools { get; }
    public double Temperature { get; }
    public double TopP { get; }
    public int MaxTokens { get; }
    public bool Stream { get; }

    public LLMRequest(
        List<LLMMessage> messages,
        double temperature = 0.7,
        double topP = 1.0,
        int maxTokens = 4096,
        bool stream = false,
        List<LLMTool>? tools = null)
    {
        if (messages == null || messages.Count == 0)
            throw new ArgumentException("Messages cannot be null or empty", nameof(messages));
        if (temperature < 0 || temperature > 2)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0 and 2");
        if (topP < 0 || topP > 1)
            throw new ArgumentOutOfRangeException(nameof(topP), "TopP must be between 0 and 1");
        if (maxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "MaxTokens must be greater than 0");

        Messages = messages;
        Temperature = temperature;
        TopP = topP;
        MaxTokens = maxTokens;
        Stream = stream;
        Tools = tools;
    }
}
```

**Tests** (Lean):
- ✅ Null/empty messages throws ArgumentException
- ✅ Temperature out of range throws ArgumentOutOfRangeException
- ✅ TopP out of range throws ArgumentOutOfRangeException
- ✅ MaxTokens <= 0 throws ArgumentOutOfRangeException
- ✅ Valid request accepts all parameters

---

##### `LLMResponse` Class
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a complete response from an LLM provider
/// </summary>
public class LLMResponse
{
    public string Content { get; }
    public List<LLMToolCall>? ToolCalls { get; }
    public string? FinishReason { get; }
    public LLMUsage? Usage { get; }

    public LLMResponse(
        string content,
        List<LLMToolCall>? toolCalls = null,
        string? finishReason = null,
        LLMUsage? usage = null)
    {
        // Content can be empty if there are tool calls
        if (string.IsNullOrEmpty(content) && (toolCalls == null || toolCalls.Count == 0))
            throw new ArgumentException("Response must have either content or tool calls");

        Content = content ?? string.Empty;
        ToolCalls = toolCalls;
        FinishReason = finishReason;
        Usage = usage;
    }
}
```

**Tests** (Lean):
- ✅ Empty content and no tool calls throws ArgumentException
- ✅ Empty content with tool calls is valid
- ✅ Content without tool calls is valid

---

##### `LLMUsage` Class
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents token usage information
/// </summary>
public class LLMUsage
{
    public int PromptTokens { get; }
    public int CompletionTokens { get; }
    public int TotalTokens { get; }

    public LLMUsage(int promptTokens, int completionTokens)
    {
        if (promptTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(promptTokens), "Prompt tokens cannot be negative");
        if (completionTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(completionTokens), "Completion tokens cannot be negative");

        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        TotalTokens = promptTokens + completionTokens;
    }
}
```

**Tests** (Lean):
- ✅ Negative promptTokens throws ArgumentOutOfRangeException
- ✅ Negative completionTokens throws ArgumentOutOfRangeException
- ✅ TotalTokens is calculated correctly

---

##### `StreamingLLMChunk` Class
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Represents a single chunk in a streaming LLM response
/// </summary>
public class StreamingLLMChunk
{
    public string ContentDelta { get; }
    public LLMToolCall? ToolCallDelta { get; }
    public bool IsComplete { get; }
    public string? FinishReason { get; }

    public StreamingLLMChunk(
        string contentDelta,
        LLMToolCall? toolCallDelta = null,
        bool isComplete = false,
        string? finishReason = null)
    {
        ContentDelta = contentDelta ?? string.Empty;
        ToolCallDelta = toolCallDelta;
        IsComplete = isComplete;
        FinishReason = finishReason;
    }
}
```

**Tests**: ❌ NO TESTS NEEDED (simple data holder, no validation)

---

### 2. Authentication (Domain + Infrastructure)

#### 2.1 Authentication Abstraction (Domain Layer)

##### `IAuthenticationProvider` Interface
```csharp
namespace TransparentAiAgentCore.Domain.Authentication;

/// <summary>
/// Provides authentication credentials for external services
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Get API key for the specified service
    /// </summary>
    string GetApiKey(string serviceName);

    /// <summary>
    /// Get endpoint URL for the specified service
    /// </summary>
    string GetEndpoint(string serviceName);
}
```

**Tests**: ❌ NO TESTS NEEDED (interface definition)

---

#### 2.2 Authentication Implementation (Infrastructure Layer)

##### `ConfigurationAuthenticationProvider` Class
```csharp
namespace TransparentAiAgentCore.Infrastructure.Authentication;

/// <summary>
/// Provides authentication credentials from configuration
/// </summary>
public class ConfigurationAuthenticationProvider : IAuthenticationProvider
{
    private readonly AppConfiguration _configuration;

    public ConfigurationAuthenticationProvider(AppConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string GetApiKey(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or whitespace", nameof(serviceName));

        return serviceName.ToLowerInvariant() switch
        {
            "azureopenai" => _configuration.LLM.AzureOpenAI?.ApiKey
                ?? throw new ConfigurationException("Azure OpenAI API key not configured"),
            "anthropic" => _configuration.LLM.Anthropic?.ApiKey
                ?? throw new ConfigurationException("Anthropic API key not configured"),
            _ => throw new ArgumentException($"Unknown service: {serviceName}", nameof(serviceName))
        };
    }

    public string GetEndpoint(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or whitespace", nameof(serviceName));

        return serviceName.ToLowerInvariant() switch
        {
            "azureopenai" => _configuration.LLM.AzureOpenAI?.Endpoint
                ?? throw new ConfigurationException("Azure OpenAI endpoint not configured"),
            "anthropic" => "https://api.anthropic.com", // Anthropic has fixed endpoint
            _ => throw new ArgumentException($"Unknown service: {serviceName}", nameof(serviceName))
        };
    }
}
```

**Tests** (Lean):
- ✅ Constructor throws ArgumentNullException for null configuration
- ✅ GetApiKey throws ArgumentException for null/empty service name
- ✅ GetApiKey returns correct API key for AzureOpenAI
- ✅ GetApiKey returns correct API key for Anthropic
- ✅ GetApiKey throws ConfigurationException when key not configured
- ✅ GetApiKey throws ArgumentException for unknown service
- ✅ GetEndpoint returns correct endpoint for AzureOpenAI
- ✅ GetEndpoint returns correct endpoint for Anthropic
- ✅ GetEndpoint throws ConfigurationException when endpoint not configured
- ✅ GetEndpoint throws ArgumentException for unknown service

---

### 3. LLM Provider Abstraction (Domain Layer)

#### 3.1 Provider Interface

##### `ILLMProvider` Interface
```csharp
namespace TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Abstraction for LLM providers
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Send a request to the LLM and get a complete response
    /// </summary>
    Task<LLMResponse> SendRequestAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a request to the LLM and stream the response
    /// </summary>
    IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(LLMRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the provider name
    /// </summary>
    string ProviderName { get; }
}
```

**Tests**: ❌ NO TESTS NEEDED (interface definition)

---

### 4. Azure OpenAI Provider (Infrastructure Layer)

#### 4.1 Azure OpenAI Implementation

##### `AzureOpenAIProvider` Class

```csharp
namespace TransparentAiAgentCore.Infrastructure.LLM;

using Azure;
using Azure.AI.OpenAI;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Exceptions;
using TransparentAiAgentCore.Domain.Authentication;

/// <summary>
/// LLM provider implementation for Azure OpenAI
/// </summary>
public class AzureOpenAIProvider : ILLMProvider
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ITransparencyService _transparencyService;

    public string ProviderName => "AzureOpenAI";

    public AzureOpenAIProvider(
        IAuthenticationProvider authProvider,
        string deploymentName,
        ITransparencyService transparencyService)
    {
        if (authProvider == null)
            throw new ArgumentNullException(nameof(authProvider));
        if (string.IsNullOrWhiteSpace(deploymentName))
            throw new ArgumentException("Deployment name cannot be null or whitespace", nameof(deploymentName));

        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _deploymentName = deploymentName;

        var endpoint = authProvider.GetEndpoint("AzureOpenAI");
        var apiKey = authProvider.GetApiKey("AzureOpenAI");

        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    // Constructor for testing with injected client
    internal AzureOpenAIProvider(
        OpenAIClient client,
        string deploymentName,
        ITransparencyService transparencyService)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
    }

    public async Task<LLMResponse> SendRequestAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Log request to transparency system
            LogRequest(request);

            // Convert to Azure OpenAI format
            var chatCompletionsOptions = BuildChatCompletionsOptions(request);

            // Send request
            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(
                _deploymentName,
                chatCompletionsOptions,
                cancellationToken);

            // Convert response
            var llmResponse = ConvertResponse(response.Value);

            // Log response to transparency system
            LogResponse(llmResponse);

            return llmResponse;
        }
        catch (RequestFailedException ex)
        {
            throw new LLMException($"Azure OpenAI request failed: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not LLMException)
        {
            throw new LLMException($"Unexpected error during LLM request: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<StreamingLLMChunk> StreamRequestAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Log request to transparency system
        LogRequest(request);

        StreamingResponse<StreamingChatCompletionsUpdate>? streamingResponse = null;

        try
        {
            // Convert to Azure OpenAI format
            var chatCompletionsOptions = BuildChatCompletionsOptions(request);

            // Get streaming response
            streamingResponse = await _client.GetChatCompletionsStreamingAsync(
                _deploymentName,
                chatCompletionsOptions,
                cancellationToken);

            // Stream chunks
            await foreach (StreamingChatCompletionsUpdate update in streamingResponse.EnumeratorWithCancellation(cancellationToken))
            {
                var chunk = ConvertStreamingUpdate(update);
                yield return chunk;
            }
        }
        catch (RequestFailedException ex)
        {
            throw new LLMException($"Azure OpenAI streaming request failed: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not LLMException)
        {
            throw new LLMException($"Unexpected error during streaming LLM request: {ex.Message}", ex);
        }
        finally
        {
            streamingResponse?.Dispose();
        }
    }

    private ChatCompletionsOptions BuildChatCompletionsOptions(LLMRequest request)
    {
        var options = new ChatCompletionsOptions
        {
            Temperature = (float)request.Temperature,
            NucleusSamplingFactor = (float)request.TopP,
            MaxTokens = request.MaxTokens
        };

        // Add messages
        foreach (var message in request.Messages)
        {
            options.Messages.Add(ConvertToAzureMessage(message));
        }

        // Add tools if present
        if (request.Tools != null && request.Tools.Count > 0)
        {
            foreach (var tool in request.Tools)
            {
                options.Tools.Add(ConvertToAzureTool(tool));
            }
        }

        return options;
    }

    private ChatRequestMessage ConvertToAzureMessage(LLMMessage message)
    {
        return message.Role.ToLowerInvariant() switch
        {
            "user" => new ChatRequestUserMessage(message.Content),
            "assistant" when message.ToolCalls != null => new ChatRequestAssistantMessage(message.Content)
            {
                ToolCalls = message.ToolCalls.Select(tc => new ChatCompletionsFunctionToolCall(tc.Id, tc.Name, tc.Arguments)).ToList<ChatCompletionsToolCall>()
            },
            "assistant" => new ChatRequestAssistantMessage(message.Content),
            "system" => new ChatRequestSystemMessage(message.Content),
            "tool" => new ChatRequestToolMessage(message.Content, message.ToolCallId!),
            _ => throw new LLMException($"Unknown message role: {message.Role}")
        };
    }

    private ChatCompletionsFunctionToolDefinition ConvertToAzureTool(LLMTool tool)
    {
        return new ChatCompletionsFunctionToolDefinition
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = BinaryData.FromString(tool.ParametersSchema)
        };
    }

    private LLMResponse ConvertResponse(ChatCompletions response)
    {
        var choice = response.Choices[0];
        var message = choice.Message;

        List<LLMToolCall>? toolCalls = null;
        if (message.ToolCalls != null && message.ToolCalls.Count > 0)
        {
            toolCalls = message.ToolCalls
                .OfType<ChatCompletionsFunctionToolCall>()
                .Select(tc => new LLMToolCall(tc.Id, tc.Name, tc.Arguments))
                .ToList();
        }

        var usage = response.Usage != null
            ? new LLMUsage(response.Usage.PromptTokens, response.Usage.CompletionTokens)
            : null;

        return new LLMResponse(
            message.Content ?? string.Empty,
            toolCalls,
            choice.FinishReason?.ToString(),
            usage);
    }

    private StreamingLLMChunk ConvertStreamingUpdate(StreamingChatCompletionsUpdate update)
    {
        var contentDelta = update.ContentUpdate ?? string.Empty;
        var isComplete = update.FinishReason != null;
        var finishReason = update.FinishReason?.ToString();

        // Handle tool calls in streaming (more complex, simplified here)
        LLMToolCall? toolCallDelta = null;
        if (update.ToolCallUpdate != null && update.ToolCallUpdate is StreamingFunctionToolCallUpdate ftc)
        {
            toolCallDelta = new LLMToolCall(
                ftc.Id ?? string.Empty,
                ftc.Name ?? string.Empty,
                ftc.ArgumentsUpdate ?? string.Empty);
        }

        return new StreamingLLMChunk(contentDelta, toolCallDelta, isComplete, finishReason);
    }

    private void LogRequest(LLMRequest request)
    {
        var eventData = _transparencyService != null
            ? System.Text.Json.JsonSerializer.Serialize(new
            {
                Provider = ProviderName,
                MessageCount = request.Messages.Count,
                Temperature = request.Temperature,
                TopP = request.TopP,
                MaxTokens = request.MaxTokens,
                ToolCount = request.Tools?.Count ?? 0,
                Stream = request.Stream
            })
            : string.Empty;

        _transparencyService?.LogEvent(
            new TransparencyEvent(
                TransparencyEventType.SystemState,
                eventData,
                "LLM Request"));
    }

    private void LogResponse(LLMResponse response)
    {
        var eventData = _transparencyService != null
            ? System.Text.Json.JsonSerializer.Serialize(new
            {
                Provider = ProviderName,
                ContentLength = response.Content?.Length ?? 0,
                ToolCallCount = response.ToolCalls?.Count ?? 0,
                FinishReason = response.FinishReason,
                Usage = response.Usage
            })
            : string.Empty;

        _transparencyService?.LogEvent(
            new TransparencyEvent(
                TransparencyEventType.AssistantResponse,
                eventData,
                "LLM Response"));
    }
}
```

**Tests** (Lean - Focus on Behavior):

**Constructor Tests:**
- ✅ Constructor throws ArgumentNullException for null authProvider
- ✅ Constructor throws ArgumentException for null/empty deploymentName
- ✅ Constructor throws ArgumentNullException for null transparencyService
- ✅ Constructor retrieves endpoint and API key from authProvider

**SendRequestAsync Tests (with mocked OpenAIClient):**
- ✅ SendRequestAsync throws ArgumentNullException for null request
- ✅ SendRequestAsync converts LLMRequest to ChatCompletionsOptions correctly
- ✅ SendRequestAsync converts Azure response to LLMResponse correctly
- ✅ SendRequestAsync logs request to transparency service
- ✅ SendRequestAsync logs response to transparency service
- ✅ SendRequestAsync wraps RequestFailedException in LLMException
- ✅ SendRequestAsync wraps unexpected exceptions in LLMException

**StreamRequestAsync Tests (with mocked OpenAIClient):**
- ✅ StreamRequestAsync throws ArgumentNullException for null request
- ✅ StreamRequestAsync yields streaming chunks correctly
- ✅ StreamRequestAsync logs request to transparency service
- ✅ StreamRequestAsync wraps RequestFailedException in LLMException
- ✅ StreamRequestAsync wraps unexpected exceptions in LLMException

**Message Conversion Tests:**
- ✅ ConvertToAzureMessage handles user messages
- ✅ ConvertToAzureMessage handles assistant messages
- ✅ ConvertToAzureMessage handles system messages
- ✅ ConvertToAzureMessage handles tool messages
- ✅ ConvertToAzureMessage handles assistant messages with tool calls
- ✅ ConvertToAzureMessage throws for unknown role

**Tool Conversion Tests:**
- ✅ ConvertToAzureTool converts LLMTool correctly

**Response Conversion Tests:**
- ✅ ConvertResponse extracts content correctly
- ✅ ConvertResponse extracts tool calls correctly
- ✅ ConvertResponse extracts finish reason correctly
- ✅ ConvertResponse extracts usage correctly

---

### 5. Provider Factory (Infrastructure Layer)

##### `LLMProviderFactory` Class

```csharp
namespace TransparentAiAgentCore.Infrastructure.LLM;

using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Exceptions;

/// <summary>
/// Factory for creating LLM provider instances
/// </summary>
public class LLMProviderFactory
{
    private readonly IAuthenticationProvider _authProvider;
    private readonly ITransparencyService _transparencyService;
    private readonly AppConfiguration _configuration;

    public LLMProviderFactory(
        IAuthenticationProvider authProvider,
        ITransparencyService transparencyService,
        AppConfiguration configuration)
    {
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
        _transparencyService = transparencyService ?? throw new ArgumentNullException(nameof(transparencyService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public ILLMProvider CreateProvider()
    {
        return CreateProvider(_configuration.LLM.Provider);
    }

    public ILLMProvider CreateProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or whitespace", nameof(providerName));

        return providerName.ToLowerInvariant() switch
        {
            "azureopenai" => CreateAzureOpenAIProvider(),
            "anthropic" => throw new NotImplementedException("Anthropic provider will be implemented in Phase 8"),
            _ => throw new ConfigurationException($"Unknown LLM provider: {providerName}")
        };
    }

    private ILLMProvider CreateAzureOpenAIProvider()
    {
        if (_configuration.LLM.AzureOpenAI == null)
            throw new ConfigurationException("Azure OpenAI configuration is missing");

        return new AzureOpenAIProvider(
            _authProvider,
            _configuration.LLM.AzureOpenAI.DeploymentName,
            _transparencyService);
    }
}
```

**Tests** (Lean):
- ✅ Constructor throws ArgumentNullException for null authProvider
- ✅ Constructor throws ArgumentNullException for null transparencyService
- ✅ Constructor throws ArgumentNullException for null configuration
- ✅ CreateProvider() uses configured provider name
- ✅ CreateProvider(string) throws ArgumentException for null/empty name
- ✅ CreateProvider("AzureOpenAI") returns AzureOpenAIProvider
- ✅ CreateProvider("Anthropic") throws NotImplementedException (Phase 8)
- ✅ CreateProvider("Unknown") throws ConfigurationException
- ✅ CreateProvider throws ConfigurationException when Azure config missing

---

### 6. Streaming Helpers (Infrastructure Layer)

##### `StreamingResponseAccumulator` Class

```csharp
namespace TransparentAiAgentCore.Infrastructure.LLM;

using TransparentAiAgentCore.Domain.LLM;

/// <summary>
/// Accumulates streaming chunks into a complete response
/// </summary>
public class StreamingResponseAccumulator
{
    private readonly StringBuilder _contentBuilder = new();
    private readonly Dictionary<string, StringBuilder> _toolCallArguments = new();
    private readonly List<LLMToolCall> _toolCalls = new();
    private string? _finishReason;

    public void AddChunk(StreamingLLMChunk chunk)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));

        // Accumulate content
        if (!string.IsNullOrEmpty(chunk.ContentDelta))
        {
            _contentBuilder.Append(chunk.ContentDelta);
        }

        // Accumulate tool calls
        if (chunk.ToolCallDelta != null)
        {
            var toolCall = chunk.ToolCallDelta;

            if (!_toolCallArguments.ContainsKey(toolCall.Id))
            {
                _toolCallArguments[toolCall.Id] = new StringBuilder();
            }

            _toolCallArguments[toolCall.Id].Append(toolCall.Arguments);
        }

        // Track finish reason
        if (chunk.IsComplete && !string.IsNullOrEmpty(chunk.FinishReason))
        {
            _finishReason = chunk.FinishReason;
        }
    }

    public LLMResponse ToResponse()
    {
        // Build complete tool calls
        List<LLMToolCall>? toolCalls = null;
        if (_toolCallArguments.Count > 0)
        {
            toolCalls = _toolCallArguments
                .Select(kvp => new LLMToolCall(kvp.Key, "extracted_name", kvp.Value.ToString()))
                .ToList();
        }

        return new LLMResponse(
            _contentBuilder.ToString(),
            toolCalls,
            _finishReason,
            null); // Usage not available during streaming
    }

    public void Reset()
    {
        _contentBuilder.Clear();
        _toolCallArguments.Clear();
        _toolCalls.Clear();
        _finishReason = null;
    }
}
```

**Tests** (Lean):
- ✅ AddChunk throws ArgumentNullException for null chunk
- ✅ AddChunk accumulates content correctly
- ✅ AddChunk accumulates tool call arguments correctly
- ✅ AddChunk tracks finish reason
- ✅ ToResponse builds complete response with content
- ✅ ToResponse builds complete response with tool calls
- ✅ Reset clears all accumulated data

---

## Implementation Order (TDD)

### Order of Implementation

1. **Domain Models - LLM Request/Response** (No dependencies)
   - LLMMessage
   - LLMToolCall
   - LLMTool
   - LLMRequest
   - LLMResponse
   - LLMUsage
   - StreamingLLMChunk

2. **Authentication Abstraction** (Depends on Configuration from Phase 1)
   - IAuthenticationProvider interface
   - ConfigurationAuthenticationProvider

3. **LLM Provider Abstraction** (Depends on domain models)
   - ILLMProvider interface

4. **Streaming Helpers** (Depends on domain models)
   - StreamingResponseAccumulator

5. **Azure OpenAI Provider** (Depends on all above)
   - AzureOpenAIProvider (with mocked Azure SDK for testing)

6. **Provider Factory** (Depends on all above)
   - LLMProviderFactory

---

## TDD Workflow

For each component:

1. **Write Test First (Red)**
   - Create test file in `TransparentAiAgentCore_Tests/Infrastructure/LLM/`
   - Write failing test(s) for component
   - Run test - should fail

2. **Implement Component (Green)**
   - Create implementation file in `TransparentAiAgentCore/Infrastructure/LLM/`
   - Write minimum code to pass test
   - Run test - should pass

3. **Refactor (Refactor)**
   - Improve code quality
   - Ensure tests still pass
   - Add more tests as needed

4. **Commit**
   - Commit tests + implementation together
   - Clear commit message

---

## Project Structure

```
TransparentAiAgentCore/
├── Domain/
│   ├── LLM/
│   │   ├── ILLMProvider.cs
│   │   ├── LLMMessage.cs
│   │   ├── LLMToolCall.cs
│   │   ├── LLMTool.cs
│   │   ├── LLMRequest.cs
│   │   ├── LLMResponse.cs
│   │   ├── LLMUsage.cs
│   │   └── StreamingLLMChunk.cs
│   └── Authentication/
│       └── IAuthenticationProvider.cs
└── Infrastructure/
    ├── Authentication/
    │   └── ConfigurationAuthenticationProvider.cs
    └── LLM/
        ├── AzureOpenAIProvider.cs
        ├── LLMProviderFactory.cs
        └── StreamingResponseAccumulator.cs

TransparentAiAgentCore_Tests/
├── Domain/
│   └── LLM/
│       ├── LLMMessageTests.cs
│       ├── LLMToolCallTests.cs
│       ├── LLMToolTests.cs
│       ├── LLMRequestTests.cs
│       ├── LLMResponseTests.cs
│       └── LLMUsageTests.cs
└── Infrastructure/
    ├── Authentication/
    │   └── ConfigurationAuthenticationProviderTests.cs
    └── LLM/
        ├── AzureOpenAIProviderTests.cs
        ├── LLMProviderFactoryTests.cs
        └── StreamingResponseAccumulatorTests.cs
```

---

## Testing Strategy

### Mocking Azure OpenAI SDK

For testing `AzureOpenAIProvider`, we need to mock the Azure OpenAI SDK. Use one of these approaches:

**Option 1: Internal Constructor for Testing**
```csharp
// Provide internal constructor that accepts OpenAIClient
internal AzureOpenAIProvider(OpenAIClient client, string deploymentName, ITransparencyService transparencyService)
```

**Option 2: Extract HTTP Client Wrapper** (More complex but cleaner)
```csharp
// Create IOpenAIClient wrapper interface
// Inject wrapper instead of concrete OpenAIClient
// Mock the wrapper in tests
```

**Recommendation**: Use Option 1 (internal constructor) for Phase 2 simplicity. Can refactor to Option 2 later if needed.

### Test Data Builders (Optional)

Consider creating test data builders for complex objects:

```csharp
public class LLMRequestBuilder
{
    private List<LLMMessage> _messages = new();
    private double _temperature = 0.7;
    // ... other fields

    public LLMRequestBuilder WithMessage(LLMMessage message)
    {
        _messages.Add(message);
        return this;
    }

    public LLMRequest Build() => new LLMRequest(_messages, _temperature, ...);
}
```

Use these in tests for cleaner setup.

---

## Dependencies

### NuGet Packages Required

Add to `TransparentAiAgentCore.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.12" />
  <PackageReference Include="System.Text.Json" Version="8.0.0" />
</ItemGroup>
```

### Phase 1 Dependencies

Phase 2 depends on Phase 1 components:
- ✅ `AppConfiguration` (for provider configuration)
- ✅ `ITransparencyService` (for logging LLM calls)
- ✅ `TransparencyEvent` (for creating events)
- ✅ `LLMException` (for error handling)
- ✅ `ConfigurationException` (for configuration errors)

**Ensure Phase 1 is 100% complete before starting Phase 2.**

---

## Deliverables

At the end of Phase 2, we will have:

✅ **LLM Integration**:
- Complete LLM provider abstraction
- Working Azure OpenAI provider with streaming
- Authentication system
- Request/response models

✅ **Capabilities**:
- Can send requests to Azure OpenAI
- Can receive streaming responses
- Can handle tool calls (function calling)
- All LLM interactions logged to transparency system

✅ **Test Coverage**:
- Unit tests for all components
- Mocked Azure SDK for provider tests
- ~95%+ code coverage
- All tests passing

✅ **Foundation Ready**:
- Phase 3 (Agent Core) can begin
- Can integrate Conversation Manager with LLM
- Ready for multi-turn conversations

---

## Next Phase

**Phase 3: Agent Core** will use these foundations to:
- Implement Conversation Manager (using LLMMessage models)
- Implement Agent Orchestrator (using ILLMProvider)
- Implement Message Pipeline (transformation logic)
- Enable multi-turn conversations with context management

---

## Common Pitfalls & Solutions

### Pitfall 1: Testing Azure SDK Directly

❌ **Bad**: Trying to test actual Azure OpenAI API calls
✅ **Good**: Mock the OpenAIClient using internal constructor

### Pitfall 2: Not Handling Streaming Errors

❌ **Bad**: Assuming streaming always succeeds
✅ **Good**: Test error handling mid-stream, use try-finally for cleanup

### Pitfall 3: Ignoring Transparency Logging

❌ **Bad**: Forgetting to log requests/responses
✅ **Good**: Log every LLM interaction to transparency system

### Pitfall 4: Not Validating Configuration

❌ **Bad**: Assuming configuration is always valid
✅ **Good**: Validate configuration and throw clear exceptions

---

## Integration Points

### With Phase 1:
- Uses `AppConfiguration` for provider settings
- Uses `ITransparencyService` for logging
- Uses exception hierarchy for errors

### With Phase 3 (Next):
- `ILLMProvider` will be used by Agent Orchestrator
- `LLMMessage` will be converted from domain `IMessage`
- Streaming will be exposed through Agent Orchestrator

---

**Status**: Ready for Implementation
**Estimated Time**: 2-3 days
**Approach**: Strict TDD - Tests First!
**Dependencies**: Phase 1 must be 100% complete

---

**Let's build the LLM integration! 🚀**
