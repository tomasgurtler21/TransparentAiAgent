# Phase 1: Foundation - Detailed Implementation Plan

**Status**: Implementation Ready
**Last Updated**: 2025-10-28

## Overview

This document provides a comprehensive, class-by-class implementation plan for Phase 1 using Test-Driven Development (TDD).

## Goals

Build the foundational infrastructure that all other components depend on:
- Domain models with context status tracking
- Exception hierarchy for error handling
- Configuration management system
- Basic transparency system for event logging
- Serialization services for data formatting

## Architecture Layer: Domain + Infrastructure

Phase 1 focuses on:
- **Domain Layer**: Core models, interfaces, enums
- **Infrastructure Layer**: Configuration, Transparency, Serialization

## Lean TDD Approach

**IMPORTANT**: We follow **Lean TDD**, not pedantic testing.

### What We DO Test:
✅ **Business Logic & Behavior**
- Validation logic (e.g., "null content throws ArgumentException")
- Constructor initialization with business meaning (e.g., "ID is generated", "ContextStatus defaults to InContext")
- Transformations (e.g., "ToolCallMessage formats Content correctly")
- Service behavior (e.g., "LogEvent adds to store and raises event")
- Error handling and edge cases
- State changes and side effects
- Uniqueness requirements (e.g., "different messages get different IDs")

### What We DON'T Test:
❌ **Compiler-Enforced or Trivial Features**
- Enum values exist (if it compiles, they exist)
- Simple property getters/setters with no logic (e.g., `public string Name { get; set; }`)
- Simple parameter-to-property assignment (e.g., `Content = content`)
- Framework features (e.g., "does Guid.NewGuid() work?")
- Properties equal themselves (e.g., `msg.Role == msg.Role`)

### Examples:

**Bad (Pedantic)**:
```csharp
[TestMethod]
public void UserMessage_Role_EqualsUser() // Waste of time - no logic, just property access
{
    var msg = new UserMessage("Hi");
    Assert.AreEqual(MessageRole.User, msg.Role);
}

[TestMethod]
public void UserMessage_Content_IsStored() // Waste of time - trivial assignment
{
    var msg = new UserMessage("Hello");
    Assert.AreEqual("Hello", msg.Content);
}
```

**Good (Lean)**:
```csharp
[TestMethod]
public void UserMessage_NullContent_ThrowsArgumentException() // Validation logic
{
    Assert.ThrowsException<ArgumentException>(() => new UserMessage(null));
}

[TestMethod]
public void UserMessage_GeneratesUniqueId() // Initialization behavior
{
    var msg1 = new UserMessage("Hi");
    var msg2 = new UserMessage("Hello");
    Assert.AreNotEqual(msg1.Id, msg2.Id);
    Assert.AreNotEqual(Guid.Empty, msg1.Id);
}

[TestMethod]
public void UserMessage_DefaultsToInContext() // Business rule
{
    var msg = new UserMessage("Hi");
    Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
}
```

### Test Specifications Below

In the specifications below, **"Tests:"** sections list only **meaningful tests**.
- If a component has **no business logic**, it may have **NO TESTS** (just implement it)
- Focus on **validation, defaults, transformations, and service behavior**

## Components & Implementation Order

### 1. Domain Models (Domain Layer)

#### 1.1 Enumerations

##### `MessageRole` Enum
```csharp
namespace TransparentAiAgentCore.Domain.Enums;

public enum MessageRole
{
    User,
    Assistant,
    Tool,
    System
}
```

**Tests**: ❌ NO TESTS NEEDED (enums compile or they don't)

---

##### `MessageContextStatus` Enum
```csharp
namespace TransparentAiAgentCore.Domain.Enums;

public enum MessageContextStatus
{
    InContext,           // Message is within LLM context window
    TruncatedFromContext // Message was removed from context (still visible in UI)
}
```

**Tests**: ❌ NO TESTS NEEDED (enums compile or they don't)

---

#### 1.2 Message Interfaces

##### `IMessage` Interface
```csharp
namespace TransparentAiAgentCore.Domain.Models;

public interface IMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Role of the message sender
    /// </summary>
    MessageRole Role { get; }

    /// <summary>
    /// Text content of the message
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Timestamp when message was created
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Whether message is currently in LLM context window
    /// </summary>
    MessageContextStatus ContextStatus { get; set; }
}
```

**Tests**: ❌ NO TESTS NEEDED (interface definition has no logic)

---

#### 1.3 Message Implementations

##### `UserMessage` Class
```csharp
namespace TransparentAiAgentCore.Domain.Models;

public class UserMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.User;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    public UserMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = Guid.NewGuid();
        Content = content;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext; // Default
    }

    // For testing/reconstruction with specific ID and timestamp
    public UserMessage(Guid id, string content, DateTime timestamp, MessageContextStatus contextStatus)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = id;
        Content = content;
        Timestamp = timestamp;
        ContextStatus = contextStatus;
    }
}
```

**Tests** (Lean - Only Meaningful Behavior):
- ✅ Null/empty content throws ArgumentException (validation)
- ✅ ContextStatus defaults to InContext (business rule)
- ✅ Generates unique IDs for different messages (initialization + uniqueness)
- ✅ ContextStatus can be updated (state change)

---

##### `AssistantMessage` Class
```csharp
namespace TransparentAiAgentCore.Domain.Models;

public class AssistantMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.Assistant;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    public AssistantMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = Guid.NewGuid();
        Content = content;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }

    // For testing/reconstruction
    public AssistantMessage(Guid id, string content, DateTime timestamp, MessageContextStatus contextStatus)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = id;
        Content = content;
        Timestamp = timestamp;
        ContextStatus = contextStatus;
    }
}
```

**Tests** (Lean - Same pattern as UserMessage):
- ✅ Null/empty content throws ArgumentException
- ✅ ContextStatus defaults to InContext
- ✅ Generates unique IDs

---

##### `SystemMessage` Class
```csharp
namespace TransparentAiAgentCore.Domain.Models;

public class SystemMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.System;
    public string Content { get; }
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    public SystemMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = Guid.NewGuid();
        Content = content;
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }

    // For testing/reconstruction
    public SystemMessage(Guid id, string content, DateTime timestamp, MessageContextStatus contextStatus)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or whitespace", nameof(content));

        Id = id;
        Content = content;
        Timestamp = timestamp;
        ContextStatus = contextStatus;
    }
}
```

**Tests** (Lean - Same pattern as UserMessage):
- ✅ Null/empty content throws ArgumentException
- ✅ ContextStatus defaults to InContext
- ✅ Generates unique IDs

---

##### `ToolCallMessage` Class
```csharp
namespace TransparentAiAgentCore.Domain.Models;

public class ToolCallMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.Tool;
    public string Content { get; } // JSON representation of tool call
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    /// <summary>
    /// Name of the tool being called
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// JSON string of tool parameters
    /// </summary>
    public string ToolParameters { get; }

    /// <summary>
    /// Unique ID for this tool call (from LLM)
    /// </summary>
    public string ToolCallId { get; }

    public ToolCallMessage(string toolName, string toolParameters, string toolCallId)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));

        Id = Guid.NewGuid();
        ToolName = toolName;
        ToolParameters = toolParameters ?? "{}";
        ToolCallId = toolCallId;
        Content = $"Tool Call: {toolName}({toolParameters})"; // Human-readable
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }

    // For testing/reconstruction
    public ToolCallMessage(Guid id, string toolName, string toolParameters, string toolCallId,
        DateTime timestamp, MessageContextStatus contextStatus)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));

        Id = id;
        ToolName = toolName;
        ToolParameters = toolParameters ?? "{}";
        ToolCallId = toolCallId;
        Content = $"Tool Call: {toolName}({toolParameters})";
        Timestamp = timestamp;
        ContextStatus = contextStatus;
    }
}
```

**Tests** (Lean - Validation + Transformation):
- ✅ Null/empty toolName throws ArgumentException (validation)
- ✅ Null/empty toolCallId throws ArgumentException (validation)
- ✅ Null toolParameters defaults to "{}" (business rule)
- ✅ Content is formatted correctly "Tool Call: toolName(params)" (transformation)
- ✅ ContextStatus defaults to InContext (business rule)

---

##### `ToolResultMessage` Class
```csharp
namespace TransparentAiAgentCore.Domain.Models;

public class ToolResultMessage : IMessage
{
    public Guid Id { get; }
    public MessageRole Role => MessageRole.Tool;
    public string Content { get; } // JSON representation of tool result
    public DateTime Timestamp { get; }
    public MessageContextStatus ContextStatus { get; set; }

    /// <summary>
    /// ID of the tool call this is responding to
    /// </summary>
    public string ToolCallId { get; }

    /// <summary>
    /// Name of the tool that was called
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Result data from the tool (JSON string)
    /// </summary>
    public string Result { get; }

    /// <summary>
    /// Whether the tool execution was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Error message if tool execution failed
    /// </summary>
    public string? ErrorMessage { get; }

    public ToolResultMessage(string toolCallId, string toolName, string result, bool isSuccess, string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));

        Id = Guid.NewGuid();
        ToolCallId = toolCallId;
        ToolName = toolName;
        Result = result ?? "{}";
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Content = isSuccess ? $"Tool Result: {result}" : $"Tool Error: {errorMessage}";
        Timestamp = DateTime.UtcNow;
        ContextStatus = MessageContextStatus.InContext;
    }

    // For testing/reconstruction
    public ToolResultMessage(Guid id, string toolCallId, string toolName, string result,
        bool isSuccess, string? errorMessage, DateTime timestamp, MessageContextStatus contextStatus)
    {
        if (string.IsNullOrWhiteSpace(toolCallId))
            throw new ArgumentException("Tool call ID cannot be null or whitespace", nameof(toolCallId));
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or whitespace", nameof(toolName));

        Id = id;
        ToolCallId = toolCallId;
        ToolName = toolName;
        Result = result ?? "{}";
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Content = isSuccess ? $"Tool Result: {result}" : $"Tool Error: {errorMessage}";
        Timestamp = timestamp;
        ContextStatus = contextStatus;
    }
}
```

**Tests** (Lean - Validation + Transformation):
- ✅ Null/empty toolCallId throws ArgumentException (validation)
- ✅ Null/empty toolName throws ArgumentException (validation)
- ✅ Null result defaults to "{}" (business rule)
- ✅ Success: Content is "Tool Result: {result}" (transformation)
- ✅ Error: Content is "Tool Error: {errorMessage}" (transformation)
- ✅ ContextStatus defaults to InContext (business rule)

---

### 2. Exception Hierarchy (Domain Layer)

#### 2.1 Base Exception

##### `AgentException` Class
```csharp
namespace TransparentAiAgentCore.Domain.Exceptions;

public class AgentException : Exception
{
    public AgentException() : base() { }

    public AgentException(string message) : base(message) { }

    public AgentException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Tests** (Lean - Basic Exception Behavior):
- ✅ Can be thrown and caught as AgentException
- ✅ Can be thrown and caught as Exception (inheritance)
- ✅ Message is preserved when provided
- ✅ InnerException is preserved when provided

---

#### 2.2 Derived Exceptions

##### `ConfigurationException` Class
```csharp
namespace TransparentAiAgentCore.Domain.Exceptions;

public class ConfigurationException : AgentException
{
    public ConfigurationException() : base() { }

    public ConfigurationException(string message) : base(message) { }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Tests**: Same pattern as AgentException

---

##### `LLMException` Class
```csharp
namespace TransparentAiAgentCore.Domain.Exceptions;

public class LLMException : AgentException
{
    public LLMException() : base() { }

    public LLMException(string message) : base(message) { }

    public LLMException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Tests**: Same pattern as AgentException

---

##### `MCPException` Class
```csharp
namespace TransparentAiAgentCore.Domain.Exceptions;

public class MCPException : AgentException
{
    public MCPException() : base() { }

    public MCPException(string message) : base(message) { }

    public MCPException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Tests**: Same pattern as AgentException

---

##### `TransparencyException` Class
```csharp
namespace TransparentAiAgentCore.Domain.Exceptions;

public class TransparencyException : AgentException
{
    public TransparencyException() : base() { }

    public TransparencyException(string message) : base(message) { }

    public TransparencyException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Tests**: Same pattern as AgentException

---

### 3. Configuration Models (Domain Layer)

#### 3.1 Configuration Classes

##### `AgentConfiguration` Class
```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

public class AgentConfiguration
{
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";
    public int ContextWindowSize { get; set; } = 20;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SystemPrompt))
            throw new ConfigurationException("SystemPrompt cannot be null or whitespace");

        if (ContextWindowSize <= 0)
            throw new ConfigurationException("ContextWindowSize must be greater than 0");
    }
}
```

**Tests** (Lean - Focus on Validation Logic):
- ✅ Validate() throws ConfigurationException for null/whitespace SystemPrompt
- ✅ Validate() throws ConfigurationException for ContextWindowSize <= 0
- ✅ Validate() succeeds for valid configuration
- ❌ NO tests for: default values, property setters (trivial)

---

##### `LLMConfiguration` Class
```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

public class LLMConfiguration
{
    public string Provider { get; set; } = "AzureOpenAI";
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
    public int MaxTokens { get; set; } = 4096;

    public AzureOpenAIConfiguration? AzureOpenAI { get; set; }
    public AnthropicConfiguration? Anthropic { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Provider))
            throw new ConfigurationException("Provider cannot be null or whitespace");

        if (Temperature < 0 || Temperature > 2)
            throw new ConfigurationException("Temperature must be between 0 and 2");

        if (TopP < 0 || TopP > 1)
            throw new ConfigurationException("TopP must be between 0 and 1");

        if (MaxTokens <= 0)
            throw new ConfigurationException("MaxTokens must be greater than 0");

        // Validate provider-specific config
        if (Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            if (AzureOpenAI == null)
                throw new ConfigurationException("AzureOpenAI configuration is required when Provider is AzureOpenAI");
            AzureOpenAI.Validate();
        }
        else if (Provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            if (Anthropic == null)
                throw new ConfigurationException("Anthropic configuration is required when Provider is Anthropic");
            Anthropic.Validate();
        }
    }
}
```

**Tests** (Lean - Focus on Validation Logic):
- ✅ Validate() throws for null/whitespace Provider
- ✅ Validate() throws for Temperature out of range (< 0 or > 2)
- ✅ Validate() throws for TopP out of range (< 0 or > 1)
- ✅ Validate() throws for MaxTokens <= 0
- ✅ Validate() throws when Azure config missing but Provider = "AzureOpenAI"
- ✅ Validate() throws when Anthropic config missing but Provider = "Anthropic"
- ✅ Validate() succeeds for valid configuration

---

##### `AzureOpenAIConfiguration` Class
```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ConfigurationException("Azure OpenAI Endpoint cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("Azure OpenAI ApiKey cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(DeploymentName))
            throw new ConfigurationException("Azure OpenAI DeploymentName cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(ApiVersion))
            throw new ConfigurationException("Azure OpenAI ApiVersion cannot be null or whitespace");

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            throw new ConfigurationException("Azure OpenAI Endpoint must be a valid URI");
    }
}
```

**Tests** (Lean - Focus on Validation Logic):
- ✅ Validate() throws for null/whitespace Endpoint
- ✅ Validate() throws for null/whitespace ApiKey
- ✅ Validate() throws for null/whitespace DeploymentName
- ✅ Validate() throws for null/whitespace ApiVersion
- ✅ Validate() throws for invalid URI format in Endpoint
- ✅ Validate() succeeds for valid configuration

---

##### `AnthropicConfiguration` Class
```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

public class AnthropicConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ConfigurationException("Anthropic ApiKey cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Model))
            throw new ConfigurationException("Anthropic Model cannot be null or whitespace");
    }
}
```

**Tests** (Lean - Focus on Validation Logic):
- ✅ Validate() throws for null/whitespace ApiKey
- ✅ Validate() throws for null/whitespace Model
- ✅ Validate() succeeds for valid configuration

---

##### `MCPConfiguration` Class
```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

public class MCPConfiguration
{
    public List<MCPServerConfiguration> Servers { get; set; } = new();

    public void Validate()
    {
        foreach (var server in Servers)
        {
            server.Validate();
        }
    }
}
```

**Tests** (Lean - Focus on Validation Logic):
- ✅ Validate() calls Validate() on each server in Servers list
- ✅ Validate() succeeds for empty Servers list
- ✅ Validate() throws if any server is invalid

---

##### `MCPServerConfiguration` Class
```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

public class MCPServerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string> Env { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ConfigurationException("MCP Server Name cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(Command))
            throw new ConfigurationException("MCP Server Command cannot be null or whitespace");
    }
}
```

**Tests** (Lean - Focus on Validation Logic):
- ✅ Validate() throws for null/whitespace Name
- ✅ Validate() throws for null/whitespace Command
- ✅ Validate() succeeds for valid configuration

---

##### `AppConfiguration` Class (Root)
```csharp
namespace TransparentAiAgentCore.Domain.Configuration;

public class AppConfiguration
{
    public AgentConfiguration Agent { get; set; } = new();
    public LLMConfiguration LLM { get; set; } = new();
    public MCPConfiguration MCP { get; set; } = new();

    public void Validate()
    {
        Agent.Validate();
        LLM.Validate();
        MCP.Validate();
    }
}
```

**Tests** (Lean - Focus on Validation Logic):
- ✅ Validate() calls Validate() on Agent, LLM, and MCP sections
- ✅ Validate() throws if any section is invalid
- ✅ Validate() succeeds for valid configuration

---

### 4. Configuration Manager (Infrastructure Layer)

#### 4.1 Configuration Service

##### `IConfigurationService` Interface
```csharp
namespace TransparentAiAgentCore.Infrastructure.Configuration;

public interface IConfigurationService
{
    /// <summary>
    /// Load configuration from default sources
    /// </summary>
    AppConfiguration LoadConfiguration();

    /// <summary>
    /// Load configuration from specific file
    /// </summary>
    AppConfiguration LoadConfiguration(string filePath);

    /// <summary>
    /// Save configuration to file
    /// </summary>
    void SaveConfiguration(AppConfiguration config, string filePath);

    /// <summary>
    /// Get current active configuration
    /// </summary>
    AppConfiguration GetConfiguration();

    /// <summary>
    /// Update configuration at runtime
    /// </summary>
    void UpdateConfiguration(AppConfiguration config);
}
```

---

##### `ConfigurationService` Class
```csharp
namespace TransparentAiAgentCore.Infrastructure.Configuration;

public class ConfigurationService : IConfigurationService
{
    private AppConfiguration _currentConfiguration;
    private readonly string _defaultConfigPath = "appsettings.json";

    public ConfigurationService()
    {
        _currentConfiguration = new AppConfiguration();
    }

    public AppConfiguration LoadConfiguration()
    {
        return LoadConfiguration(_defaultConfigPath);
    }

    public AppConfiguration LoadConfiguration(string filePath)
    {
        // Load from file using System.Text.Json
        // Merge with defaults
        // Validate
        // Return configuration
        // Implementation details in actual code
        throw new NotImplementedException();
    }

    public void SaveConfiguration(AppConfiguration config, string filePath)
    {
        // Validate first
        config.Validate();

        // Serialize to JSON
        // Write to file
        // Implementation details in actual code
        throw new NotImplementedException();
    }

    public AppConfiguration GetConfiguration()
    {
        return _currentConfiguration;
    }

    public void UpdateConfiguration(AppConfiguration config)
    {
        config.Validate();
        _currentConfiguration = config;
    }
}
```

**Tests** (Lean - Focus on Service Behavior):
- ✅ GetConfiguration() returns current configuration
- ✅ UpdateConfiguration() updates current configuration
- ✅ UpdateConfiguration() validates before updating (throws if invalid)
- ✅ SaveConfiguration() validates before saving (throws if invalid)
- 📝 LoadConfiguration() and SaveConfiguration() file I/O can be deferred or mocked for Phase 1
- ❌ NO test for: Constructor (trivial initialization)

---

### 5. Transparency System (Infrastructure Layer)

#### 5.1 Transparency Event Models

##### `TransparencyEventType` Enum
```csharp
namespace TransparentAiAgentCore.Domain.Transparency;

public enum TransparencyEventType
{
    UserInput,
    AssistantResponse,
    ToolCall,
    ToolResult,
    ContextChange,
    ConfigurationChange,
    SystemState,
    Error
}
```

---

##### `TransparencyEvent` Class
```csharp
namespace TransparentAiAgentCore.Domain.Transparency;

public class TransparencyEvent
{
    public Guid Id { get; }
    public TransparencyEventType EventType { get; }
    public DateTime Timestamp { get; }
    public string Data { get; } // JSON serialized data
    public string? AdditionalInfo { get; }

    public TransparencyEvent(TransparencyEventType eventType, string data, string? additionalInfo = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        AdditionalInfo = additionalInfo;
        Timestamp = DateTime.UtcNow;
    }

    // For testing/reconstruction
    public TransparencyEvent(Guid id, TransparencyEventType eventType, DateTime timestamp,
        string data, string? additionalInfo = null)
    {
        Id = id;
        EventType = eventType;
        Timestamp = timestamp;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        AdditionalInfo = additionalInfo;
    }
}
```

**Tests** (Lean - Initialization + Validation):
- ✅ Generates unique ID for different events
- ✅ Timestamp set to UtcNow (approximately)
- ✅ Null data throws ArgumentNullException
- ❌ NO tests for: simple property preservation (trivial)

---

#### 5.2 Transparency Service

##### `ITransparencyService` Interface
```csharp
namespace TransparentAiAgentCore.Infrastructure.Transparency;

public interface ITransparencyService
{
    /// <summary>
    /// Log a transparency event
    /// </summary>
    void LogEvent(TransparencyEvent evt);

    /// <summary>
    /// Get all transparency events
    /// </summary>
    IEnumerable<TransparencyEvent> GetEvents();

    /// <summary>
    /// Get transparency events by type
    /// </summary>
    IEnumerable<TransparencyEvent> GetEventsByType(TransparencyEventType eventType);

    /// <summary>
    /// Get transparency events within time range
    /// </summary>
    IEnumerable<TransparencyEvent> GetEventsByTimeRange(DateTime start, DateTime end);

    /// <summary>
    /// Clear all events (for testing)
    /// </summary>
    void ClearEvents();

    /// <summary>
    /// Event raised when new transparency event is logged
    /// </summary>
    event EventHandler<TransparencyEvent>? EventLogged;
}
```

---

##### `TransparencyService` Class
```csharp
namespace TransparentAiAgentCore.Infrastructure.Transparency;

public class TransparencyService : ITransparencyService
{
    private readonly List<TransparencyEvent> _events = new();
    private readonly object _lock = new();

    public event EventHandler<TransparencyEvent>? EventLogged;

    public void LogEvent(TransparencyEvent evt)
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));

        lock (_lock)
        {
            _events.Add(evt);
        }

        // Raise event
        EventLogged?.Invoke(this, evt);
    }

    public IEnumerable<TransparencyEvent> GetEvents()
    {
        lock (_lock)
        {
            return _events.ToList(); // Return copy
        }
    }

    public IEnumerable<TransparencyEvent> GetEventsByType(TransparencyEventType eventType)
    {
        lock (_lock)
        {
            return _events.Where(e => e.EventType == eventType).ToList();
        }
    }

    public IEnumerable<TransparencyEvent> GetEventsByTimeRange(DateTime start, DateTime end)
    {
        lock (_lock)
        {
            return _events.Where(e => e.Timestamp >= start && e.Timestamp <= end).ToList();
        }
    }

    public void ClearEvents()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
```

**Tests** (Lean - Service Behavior):
- ✅ LogEvent() adds event to store
- ✅ LogEvent() raises EventLogged event
- ✅ LogEvent() throws ArgumentNullException for null event
- ✅ GetEvents() returns all logged events
- ✅ GetEvents() returns copy, not original list (defensive copy)
- ✅ GetEventsByType() filters correctly
- ✅ GetEventsByTimeRange() filters correctly
- ✅ ClearEvents() removes all events
- ✅ Thread safety: multiple threads can log concurrently without corruption

---

### 6. Serialization Service (Infrastructure Layer)

#### 6.1 Serialization Service

##### `ISerializationService` Interface
```csharp
namespace TransparentAiAgentCore.Infrastructure.Serialization;

public interface ISerializationService
{
    /// <summary>
    /// Serialize object to JSON string
    /// </summary>
    string SerializeToJson<T>(T obj);

    /// <summary>
    /// Serialize object to pretty-printed JSON
    /// </summary>
    string SerializeToPrettyJson<T>(T obj);

    /// <summary>
    /// Deserialize JSON string to object
    /// </summary>
    T DeserializeFromJson<T>(string json);
}
```

---

##### `SerializationService` Class
```csharp
namespace TransparentAiAgentCore.Infrastructure.Serialization;

using System.Text.Json;

public class SerializationService : ISerializationService
{
    private readonly JsonSerializerOptions _standardOptions;
    private readonly JsonSerializerOptions _prettyOptions;

    public SerializationService()
    {
        _standardOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _prettyOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public string SerializeToJson<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return JsonSerializer.Serialize(obj, _standardOptions);
    }

    public string SerializeToPrettyJson<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return JsonSerializer.Serialize(obj, _prettyOptions);
    }

    public T DeserializeFromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or whitespace", nameof(json));

        var result = JsonSerializer.Deserialize<T>(json, _standardOptions);
        if (result == null)
            throw new InvalidOperationException("Deserialization resulted in null");

        return result;
    }
}
```

**Tests** (Lean - Service Behavior):
- ✅ SerializeToJson() produces valid JSON with camelCase (not indented)
- ✅ SerializeToPrettyJson() produces indented JSON
- ✅ DeserializeFromJson() reconstructs object correctly
- ✅ Round-trip serialization maintains data integrity
- ✅ SerializeToJson() throws ArgumentNullException for null
- ✅ DeserializeFromJson() throws ArgumentException for null/empty JSON
- ❌ NO separate tests for "uses camelCase" vs "is indented" (combined with behavior tests)

---

## Implementation Order (TDD)

### Order of Implementation

1. **Domain Models - Enums** (Simplest, no dependencies)
   - MessageRole
   - MessageContextStatus
   - TransparencyEventType

2. **Domain Models - Message Interface & Implementations**
   - IMessage interface
   - UserMessage
   - AssistantMessage
   - SystemMessage
   - ToolCallMessage
   - ToolResultMessage

3. **Exception Hierarchy** (No dependencies)
   - AgentException
   - ConfigurationException
   - LLMException
   - MCPException
   - TransparencyException

4. **Configuration Models** (Depends on ConfigurationException)
   - AgentConfiguration
   - AzureOpenAIConfiguration
   - AnthropicConfiguration
   - LLMConfiguration
   - MCPServerConfiguration
   - MCPConfiguration
   - AppConfiguration

5. **Serialization Service** (No domain dependencies)
   - ISerializationService interface
   - SerializationService implementation

6. **Configuration Service** (Depends on Configuration Models, Serialization)
   - IConfigurationService interface
   - ConfigurationService implementation

7. **Transparency System** (Depends on TransparencyEvent, TransparencyException)
   - TransparencyEvent model
   - ITransparencyService interface
   - TransparencyService implementation

---

## TDD Workflow

For each component:

1. **Write Test First (Red)**
   - Create test file in `TransparentAiAgentCore_Tests`
   - Write failing test(s) for component
   - Run test - should fail

2. **Implement Component (Green)**
   - Create implementation file in `TransparentAiAgentCore`
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
│   ├── Models/
│   │   ├── IMessage.cs
│   │   ├── UserMessage.cs
│   │   ├── AssistantMessage.cs
│   │   ├── SystemMessage.cs
│   │   ├── ToolCallMessage.cs
│   │   └── ToolResultMessage.cs
│   ├── Enums/
│   │   ├── MessageRole.cs
│   │   └── MessageContextStatus.cs
│   ├── Exceptions/
│   │   ├── AgentException.cs
│   │   ├── ConfigurationException.cs
│   │   ├── LLMException.cs
│   │   ├── MCPException.cs
│   │   └── TransparencyException.cs
│   ├── Configuration/
│   │   ├── AppConfiguration.cs
│   │   ├── AgentConfiguration.cs
│   │   ├── LLMConfiguration.cs
│   │   ├── AzureOpenAIConfiguration.cs
│   │   ├── AnthropicConfiguration.cs
│   │   ├── MCPConfiguration.cs
│   │   └── MCPServerConfiguration.cs
│   └── Transparency/
│       ├── TransparencyEvent.cs
│       └── TransparencyEventType.cs
└── Infrastructure/
    ├── Configuration/
    │   ├── IConfigurationService.cs
    │   └── ConfigurationService.cs
    ├── Transparency/
    │   ├── ITransparencyService.cs
    │   └── TransparencyService.cs
    └── Serialization/
        ├── ISerializationService.cs
        └── SerializationService.cs

TransparentAiAgentCore_Tests/
├── Domain/
│   ├── Models/
│   │   ├── UserMessageTests.cs
│   │   ├── AssistantMessageTests.cs
│   │   ├── SystemMessageTests.cs
│   │   ├── ToolCallMessageTests.cs
│   │   └── ToolResultMessageTests.cs
│   ├── Exceptions/
│   │   └── ExceptionTests.cs
│   └── Configuration/
│       ├── AgentConfigurationTests.cs
│       ├── LLMConfigurationTests.cs
│       ├── AzureOpenAIConfigurationTests.cs
│       ├── AnthropicConfigurationTests.cs
│       ├── MCPConfigurationTests.cs
│       └── AppConfigurationTests.cs
└── Infrastructure/
    ├── Configuration/
    │   └── ConfigurationServiceTests.cs
    ├── Transparency/
    │   └── TransparencyServiceTests.cs
    └── Serialization/
        └── SerializationServiceTests.cs
```

---

## Deliverables

At the end of Phase 1, we will have:

✅ **Domain Models**:
- All message types with context status tracking
- Complete exception hierarchy
- All configuration models with validation

✅ **Infrastructure Services**:
- Configuration loading and saving
- Basic transparency event logging
- JSON serialization/deserialization

✅ **Test Coverage**:
- Unit tests for all components
- ~95%+ code coverage
- All tests passing

✅ **Foundation Ready**:
- Phase 2 (LLM Integration) can begin
- Clean architecture maintained
- TDD discipline established

---

## Next Phase

**Phase 2: LLM Integration** will use these foundations to:
- Implement `ILLMProvider` interface (using domain models)
- Use `AzureOpenAIConfiguration` from Phase 1
- Log LLM calls to `TransparencyService`
- Use message models for conversation

---

**Status**: Ready for Implementation
**Estimated Time**: 1-2 days
**Approach**: Strict TDD - Tests First!
