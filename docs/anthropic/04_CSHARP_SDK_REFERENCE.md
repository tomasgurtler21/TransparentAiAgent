# C# SDK Reference

**Last Updated:** January 2025
**SDK:** Official Anthropic C# SDK (Beta)
**GitHub:** https://github.com/anthropics/anthropic-sdk-csharp

---

## Installation

### From Local Clone (Current Method)
```bash
cd C:\programming\TransparentAiAgent
git clone https://github.com/anthropics/anthropic-sdk-csharp.git
```

Add to `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\anthropic-sdk-csharp\src\Anthropic.Client\Anthropic.Client.csproj" />
</ItemGroup>
```

### Future NuGet (When Available)
```bash
dotnet add package Anthropic.SDK
```

---

## Client Configuration

### Via Environment Variables
```csharp
// Reads from ANTHROPIC_API_KEY, ANTHROPIC_AUTH_TOKEN, ANTHROPIC_BASE_URL
AnthropicClient client = new();
```

### Via Properties
```csharp
AnthropicClient client = new()
{
    APIKey = "sk-ant-...",
    BaseUrl = "https://api.anthropic.com"
};
```

---

## Key Classes

### MessageCreateParams
```csharp
var parameters = new MessageCreateParams
{
    Model = "claude-sonnet-4-5-20250929",
    Messages = messages,
    MaxTokens = 1024,
    Temperature = 0.7,
    TopP = 0.9,
    System = new SystemModel("You are helpful"),
    Tools = tools,
    Stream = false
};
```

### MessageParam
```csharp
var message = new MessageParam
{
    Role = Role.User,  // or Role.Assistant
    Content = new Content("Hello, Claude")
};
```

### Tool Definition
```csharp
var tool = new Tool
{
    Name = "get_weather",
    Description = "Get the current weather",
    InputSchema = new InputSchema
    {
        Type = JsonSerializer.Deserialize<JsonElement>("\"object\""),
        Properties1 = JsonSerializer.Deserialize<JsonElement>(
            @"{""location"": {""type"": ""string""}}"
        ),
        Required = new List<string> { "location" }
    }
};

var toolUnion = new ToolUnion(tool);
```

---

## Non-Streaming Request

```csharp
Message response = await client.Messages.Create(parameters);

// Access response
string messageId = response.ID;
string stopReason = response.StopReason?.ToString() ?? "unknown";

// Extract content
string text = string.Empty;
foreach (var contentBlock in response.Content)
{
    if (contentBlock.TryPickText(out var textBlock))
    {
        text += textBlock.Text;
    }
}

// Extract usage
int inputTokens = response.Usage?.InputTokens ?? 0;
int outputTokens = response.Usage?.OutputTokens ?? 0;
```

---

## Streaming Request

```csharp
IAsyncEnumerable<RawMessageStreamEvent> stream =
    client.Messages.CreateStreaming(parameters);

await foreach (var evt in stream)
{
    if (evt.TryPickContentBlockDelta(out var delta))
    {
        if (delta.Delta.TryPickText(out var textDelta))
        {
            Console.Write(textDelta.Text);
        }
    }
    else if (evt.TryPickStop(out _))
    {
        Console.WriteLine("\n[Complete]");
    }
}
```

---

## Event Type Checking

```csharp
if (evt.TryPickMessageStart(out var start)) { /* ... */ }
else if (evt.TryPickContentBlockStart(out var blockStart)) { /* ... */ }
else if (evt.TryPickContentBlockDelta(out var delta)) { /* ... */ }
else if (evt.TryPickContentBlockStop(out var stop)) { /* ... */ }
else if (evt.TryPickMessageDelta(out var msgDelta)) { /* ... */ }
else if (evt.TryPickStop(out var stopEvt)) { /* ... */ }
else if (evt.TryPickPing(out _)) { /* ignore */ }
```

---

## Exception Hierarchy

```csharp
try
{
    var response = await client.Messages.Create(parameters);
}
catch (AnthropicBadRequestException ex) { /* 400 */ }
catch (AnthropicUnauthorizedException ex) { /* 401 */ }
catch (AnthropicForbiddenException ex) { /* 403 */ }
catch (AnthropicNotFoundException ex) { /* 404 */ }
catch (AnthropicUnprocessableEntityException ex) { /* 422 */ }
catch (AnthropicRateLimitException ex) { /* 429 - implement retry */ }
catch (Anthropic5xxException ex) { /* 500-599 - server error */ }
catch (AnthropicSseException ex) { /* streaming error */ }
catch (AnthropicIOException ex) { /* network error */ }
catch (AnthropicInvalidDataException ex) { /* data interpretation */ }
catch (AnthropicException ex) { /* base exception */ }
```

---

## Additional Resources

- [01_API_COMPLETE_REFERENCE.md](01_API_COMPLETE_REFERENCE.md)
- [03_STREAMING_IMPLEMENTATION_GUIDE.md](03_STREAMING_IMPLEMENTATION_GUIDE.md)
- [06_COMMON_ISSUES_AND_DEBUGGING.md](06_COMMON_ISSUES_AND_DEBUGGING.md)
