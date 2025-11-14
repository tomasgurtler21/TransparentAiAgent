# Conversation History - Implementation Plan

**Created**: 2025-11-14
**Status**: Ready for Implementation
**Design Document**: `CONVERSATION_HISTORY_DESIGN.md` (100% Complete)

---

## Overview

This document breaks down the conversation history implementation into **5 independent sessions**, each following **Lean TDD** principles. Each session is self-contained and produces testable, deliverable code.

**Design Reference**: All technical details are in `CONVERSATION_HISTORY_DESIGN.md` sections 11.1-11.5.

---

## Implementation Approach

### TDD Workflow (Per `.claude/skills/tdd/SKILL.md`)

Each session follows **Red → Green → Refactor**:

1. **RED**: Write test first → Add minimal stubs to compile → Run test → Verify it FAILS for the right reason
2. **GREEN**: Write minimum code to make test pass
3. **REFACTOR**: Improve code while keeping tests green

**Critical Discipline**: Always investigate unexpected test results (passes when should fail, or fails for wrong reason).

### Session Independence

Each session:
- ✅ Can be completed independently
- ✅ Produces working, tested code
- ✅ Has clear entry/exit criteria
- ✅ Can survive crashes (results are committed)

**Between Sessions**: Always commit and push all changes.

---

## Session 1: Domain Layer - Conversation Entities

**Goal**: Implement domain entities for conversation history.

**Dependencies**: None (pure domain layer)

**Files to Create** (with TDD):

1. `TransparentAiAgentCore/Domain/ConversationHistory/Conversation.cs`
2. `TransparentAiAgentCore/Domain/ConversationHistory/ConversationMetadata.cs`
3. `TransparentAiAgentCore/Domain/ConversationHistory/ConversationConfiguration.cs`
4. `TransparentAiAgentCore/Domain/ConversationHistory/IConversationRepository.cs`

**Test File to Create**:

1. `TransparentAiAgentCore_Tests/Domain/ConversationHistory/ConversationTests.cs`
2. `TransparentAiAgentCore_Tests/Domain/ConversationHistory/ConversationConfigurationTests.cs`

### What to Test (Lean TDD)

**✅ Test These** (meaningful behavior):
- `Conversation.GenerateName()` - Logic with transformations
  - Extracts first user message content
  - Truncates to 50 characters
  - Falls back to timestamp if no user message
  - Sanitizes invalid filename characters
- `Conversation.SanitizeFileName()` - Transformation logic
  - Removes invalid characters
  - Handles edge cases (empty, all-invalid, etc.)
- `ConversationConfiguration.CreateSnapshot()` - Complex transformation
  - Copies all relevant fields from AppConfiguration
  - Excludes sensitive data (API keys, TenantId)
  - Handles Anthropic vs Azure OpenAI conditionally
  - Handles null ExtendedThinking configuration

**❌ Skip These** (no logic):
- Simple property getters/setters (ConversationId, Name, CreatedAt, etc.)
- ConversationMetadata properties (pure DTO)
- IConversationRepository interface (no implementation)

### Test Examples

```csharp
[TestClass]
public class ConversationTests
{
    [TestMethod]
    public void GenerateName_WithUserMessage_ExtractsContent()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new UserMessage("What is clean architecture?")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.AreEqual("What is clean architecture?", name);
    }

    [TestMethod]
    public void GenerateName_WithLongMessage_TruncatesTo50Characters()
    {
        // Arrange
        var longMessage = new string('a', 100);
        var messages = new List<IMessage> { new UserMessage(longMessage) };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.AreEqual(53, name.Length); // 50 + "..."
        Assert.IsTrue(name.EndsWith("..."));
    }

    [TestMethod]
    public void GenerateName_WithInvalidFileNameChars_Sanitizes()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new UserMessage("File<>:|?*/name")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.IsFalse(name.Contains('<'));
        Assert.IsFalse(name.Contains('>'));
        // Verify it uses underscores or removes invalid chars
    }

    [TestMethod]
    public void GenerateName_NoUserMessage_FallsBackToTimestamp()
    {
        // Arrange
        var messages = new List<IMessage>
        {
            new AssistantMessage("Hello")
        };

        // Act
        var name = Conversation.GenerateName(messages);

        // Assert
        Assert.IsTrue(name.StartsWith("Conversation "));
        // Verify timestamp format
    }
}

[TestClass]
public class ConversationConfigurationTests
{
    [TestMethod]
    public void CreateSnapshot_AnthropicProvider_ExcludesApiKey()
    {
        // Arrange
        var appConfig = CreateAnthropicAppConfig();

        // Act
        var snapshot = ConversationConfiguration.CreateSnapshot(appConfig);

        // Assert
        Assert.IsNotNull(snapshot.Anthropic);
        Assert.AreEqual("claude-sonnet-4-5", snapshot.Anthropic.Model);
        // Verify API key is NOT in snapshot (check serialized JSON doesn't contain it)
    }

    [TestMethod]
    public void CreateSnapshot_AzureOpenAIProvider_ExcludesApiKeyAndTenantId()
    {
        // Arrange
        var appConfig = CreateAzureOpenAIAppConfig();

        // Act
        var snapshot = ConversationConfiguration.CreateSnapshot(appConfig);

        // Assert
        Assert.IsNotNull(snapshot.AzureOpenAI);
        Assert.AreEqual("https://my-endpoint", snapshot.AzureOpenAI.Endpoint);
        // Verify API key and TenantId are NOT in snapshot
    }

    [TestMethod]
    public void CreateSnapshot_CopiesAgentConfiguration()
    {
        // Arrange
        var appConfig = CreateAppConfig();
        appConfig.Agent.SystemPrompt = "Test prompt";
        appConfig.Agent.ContextWindowSize = 50;

        // Act
        var snapshot = ConversationConfiguration.CreateSnapshot(appConfig);

        // Assert
        Assert.AreEqual("Test prompt", snapshot.SystemPrompt);
        Assert.AreEqual(50, snapshot.ContextWindowSize);
    }
}
```

### Implementation Order (TDD)

1. **Create test file structure**
2. **Write tests for `Conversation.GenerateName()`** (RED)
3. **Implement `Conversation.GenerateName()`** (GREEN)
4. **Refactor** name generation logic (REFACTOR)
5. **Write tests for `ConversationConfiguration.CreateSnapshot()`** (RED)
6. **Implement snapshot creation** (GREEN)
7. **Refactor** snapshot logic if needed (REFACTOR)
8. **Create other classes** (ConversationMetadata, IConversationRepository - no tests needed, pure DTOs/interfaces)

### Exit Criteria

- ✅ All domain classes created
- ✅ All meaningful behavior tested
- ✅ All tests passing
- ✅ Code committed and pushed

**Estimated Complexity**: Low (pure domain logic, no dependencies)

---

## Session 2: Infrastructure Layer - JSON Repository

**Goal**: Implement file-based conversation persistence.

**Dependencies**: Session 1 (domain entities)

**Files to Create** (with TDD):

1. `TransparentAiAgentCore/Infrastructure/ConversationHistory/JsonConversationRepository.cs`

**Test File to Create**:

1. `TransparentAiAgentCore_Tests/Infrastructure/ConversationHistory/JsonConversationRepositoryTests.cs`

### What to Test (Lean TDD)

**✅ Test These** (meaningful behavior):
- `SaveAsync()` - File creation and persistence
  - Creates conversation file with correct name format
  - Serializes conversation correctly
  - Updates LastModifiedAt timestamp
  - Overwrites existing file
  - Handles directory creation
- `LoadAsync()` - File reading and deserialization
  - Loads conversation from file
  - Deserializes messages correctly
  - Throws FileNotFoundException if file missing
  - Throws InvalidOperationException if JSON corrupted
- `ListAllAsync()` - Directory scanning and metadata extraction
  - Lists all conversation files
  - Extracts metadata without loading full messages
  - Sorts by LastModifiedAt descending
  - Skips corrupted files with warning
  - Returns empty list if no conversations
- `DeleteAsync()` - File deletion
  - Deletes conversation file
  - Throws FileNotFoundException if file missing
- `ExistsAsync()` - File existence check
  - Returns true if file exists
  - Returns false if file doesn't exist
- `GetFilePath()` - File path resolution
  - Finds file by conversationId pattern
  - Returns default path for new conversations
- `SanitizeFileName()` - Filename sanitization
  - Removes invalid characters
  - Limits length to 50 characters
  - Handles empty/null input

**❌ Skip These** (framework behavior):
- JSON serialization itself (framework feature)
- File I/O primitives (framework feature)

### Test Examples

```csharp
[TestClass]
public class JsonConversationRepositoryTests
{
    private string _testDirectory;
    private IMessageSerializer _messageSerializer;
    private ILogger<JsonConversationRepository> _logger;
    private JsonConversationRepository _repository;

    [TestInitialize]
    public void Setup()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Setup dependencies
        _messageSerializer = new MessageSerializer();
        _logger = new Mock<ILogger<JsonConversationRepository>>().Object;

        // Create repository pointing to test directory
        _repository = new JsonConversationRepository(_messageSerializer, _logger, _testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task SaveAsync_NewConversation_CreatesFile()
    {
        // Arrange
        var conversation = CreateTestConversation();

        // Act
        await _repository.SaveAsync(conversation);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.AreEqual(1, files.Length);
        Assert.IsTrue(files[0].Contains(conversation.ConversationId.ToString()));
    }

    [TestMethod]
    public async Task SaveAsync_UpdatesLastModifiedAt()
    {
        // Arrange
        var conversation = CreateTestConversation();
        var originalTime = conversation.LastModifiedAt;
        await Task.Delay(100); // Ensure time difference

        // Act
        await _repository.SaveAsync(conversation);

        // Assert
        Assert.IsTrue(conversation.LastModifiedAt > originalTime);
    }

    [TestMethod]
    public async Task LoadAsync_ExistingConversation_ReturnsConversation()
    {
        // Arrange
        var original = CreateTestConversation();
        await _repository.SaveAsync(original);

        // Act
        var loaded = await _repository.LoadAsync(original.ConversationId);

        // Assert
        Assert.AreEqual(original.ConversationId, loaded.ConversationId);
        Assert.AreEqual(original.Name, loaded.Name);
        Assert.AreEqual(original.Messages.Count, loaded.Messages.Count);
    }

    [TestMethod]
    public async Task LoadAsync_MissingConversation_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _repository.LoadAsync(nonExistentId)
        );
    }

    [TestMethod]
    public async Task ListAllAsync_MultipleConversations_SortsByMostRecent()
    {
        // Arrange
        var conv1 = CreateTestConversation();
        conv1.LastModifiedAt = DateTime.UtcNow.AddHours(-2);
        await _repository.SaveAsync(conv1);

        var conv2 = CreateTestConversation();
        conv2.LastModifiedAt = DateTime.UtcNow.AddHours(-1);
        await _repository.SaveAsync(conv2);

        var conv3 = CreateTestConversation();
        conv3.LastModifiedAt = DateTime.UtcNow;
        await _repository.SaveAsync(conv3);

        // Act
        var list = await _repository.ListAllAsync();

        // Assert
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual(conv3.ConversationId, list[0].ConversationId); // Most recent first
        Assert.AreEqual(conv2.ConversationId, list[1].ConversationId);
        Assert.AreEqual(conv1.ConversationId, list[2].ConversationId);
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingConversation_RemovesFile()
    {
        // Arrange
        var conversation = CreateTestConversation();
        await _repository.SaveAsync(conversation);

        // Act
        await _repository.DeleteAsync(conversation.ConversationId);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.AreEqual(0, files.Length);
    }

    [TestMethod]
    public async Task ExistsAsync_ExistingConversation_ReturnsTrue()
    {
        // Arrange
        var conversation = CreateTestConversation();
        await _repository.SaveAsync(conversation);

        // Act
        var exists = await _repository.ExistsAsync(conversation.ConversationId);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_MissingConversation_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        Assert.IsFalse(exists);
    }

    private Conversation CreateTestConversation()
    {
        return new Conversation
        {
            ConversationId = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            Configuration = new ConversationConfiguration(),
            Messages = new List<IMessage>
            {
                new UserMessage("Test message")
            }
        };
    }
}
```

### Implementation Order (TDD)

1. **Create test file and setup infrastructure** (temp directory, cleanup)
2. **Write tests for `SaveAsync()`** (RED)
3. **Implement `SaveAsync()`** (GREEN)
4. **Write tests for `LoadAsync()`** (RED)
5. **Implement `LoadAsync()`** (GREEN)
6. **Write tests for `ListAllAsync()`** (RED)
7. **Implement `ListAllAsync()`** (GREEN)
8. **Write tests for `DeleteAsync()` and `ExistsAsync()`** (RED)
9. **Implement `DeleteAsync()` and `ExistsAsync()`** (GREEN)
10. **Refactor** common code, improve error handling (REFACTOR)

### Exit Criteria

- ✅ JsonConversationRepository fully implemented
- ✅ All repository operations tested
- ✅ All tests passing
- ✅ Error handling verified (missing files, corrupted JSON)
- ✅ Code committed and pushed

**Estimated Complexity**: Medium (file I/O, JSON serialization, error handling)

---

## Session 3: Application Layer - History Manager

**Goal**: Implement conversation history orchestration service.

**Dependencies**: Sessions 1 & 2 (domain + repository)

**Files to Create** (with TDD):

1. `TransparentAiAgentCore/Application/ConversationHistory/IConversationHistoryManager.cs`
2. `TransparentAiAgentCore/Application/ConversationHistory/ConversationHistoryManager.cs`

**Test File to Create**:

1. `TransparentAiAgentCore_Tests/Application/ConversationHistory/ConversationHistoryManagerTests.cs`

### What to Test (Lean TDD)

**✅ Test These** (meaningful behavior):
- `SaveCurrentConversationAsync()` - Orchestration logic
  - Skips empty conversations (no messages)
  - Generates name from messages
  - Creates configuration snapshot
  - Preserves CreatedAt for existing conversations
  - Calls repository.SaveAsync()
  - Handles save failures gracefully (logs, doesn't throw)
- `LoadConversationAsync()` - Pass-through with logging
  - Calls repository.LoadAsync()
  - Logs load operation
  - Propagates exceptions
- `GetConversationListAsync()` - Pass-through
  - Calls repository.ListAllAsync()
  - Returns sorted list
- `CreateNewConversationAsync()` - ID generation
  - Returns new GUID
  - Doesn't save immediately (waits for first message)
- `DeleteConversationAsync()` - Pass-through
  - Calls repository.DeleteAsync()
  - Logs deletion

**❌ Skip These**:
- IConversationHistoryManager interface (no implementation)

### Test Examples

```csharp
[TestClass]
public class ConversationHistoryManagerTests
{
    private Mock<IConversationRepository> _mockRepository;
    private Mock<ILogger<ConversationHistoryManager>> _mockLogger;
    private ConversationHistoryManager _manager;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IConversationRepository>();
        _mockLogger = new Mock<ILogger<ConversationHistoryManager>>();
        _manager = new ConversationHistoryManager(_mockRepository.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_EmptyMessages_SkipsSave()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage>();
        var config = CreateTestAppConfig();

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages, config);

        // Assert
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_WithMessages_CallsRepository()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new UserMessage("Test") };
        var config = CreateTestAppConfig();

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages, config);

        // Assert
        _mockRepository.Verify(
            r => r.SaveAsync(It.Is<Conversation>(c =>
                c.ConversationId == conversationId &&
                c.Messages.Count == 1
            )),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_ExistingConversation_PreservesCreatedAt()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new UserMessage("Test") };
        var config = CreateTestAppConfig();

        var existingConversation = new Conversation
        {
            ConversationId = conversationId,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        _mockRepository.Setup(r => r.ExistsAsync(conversationId))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.LoadAsync(conversationId))
            .ReturnsAsync(existingConversation);

        // Act
        await _manager.SaveCurrentConversationAsync(conversationId, messages, config);

        // Assert
        _mockRepository.Verify(
            r => r.SaveAsync(It.Is<Conversation>(c =>
                c.CreatedAt == existingConversation.CreatedAt
            )),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SaveCurrentConversationAsync_SaveFails_DoesNotThrow()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var messages = new List<IMessage> { new UserMessage("Test") };
        var config = CreateTestAppConfig();

        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Conversation>()))
            .ThrowsAsync(new IOException("Disk full"));

        // Act - should not throw
        await _manager.SaveCurrentConversationAsync(conversationId, messages, config);

        // Assert - verify error was logged
        // (Use logger verification if needed)
    }

    [TestMethod]
    public async Task LoadConversationAsync_CallsRepository()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var expectedConversation = new Conversation { ConversationId = conversationId };
        _mockRepository.Setup(r => r.LoadAsync(conversationId))
            .ReturnsAsync(expectedConversation);

        // Act
        var result = await _manager.LoadConversationAsync(conversationId);

        // Assert
        Assert.AreEqual(conversationId, result.ConversationId);
        _mockRepository.Verify(r => r.LoadAsync(conversationId), Times.Once);
    }

    [TestMethod]
    public async Task CreateNewConversationAsync_ReturnsNewGuid()
    {
        // Act
        var id1 = await _manager.CreateNewConversationAsync();
        var id2 = await _manager.CreateNewConversationAsync();

        // Assert
        Assert.AreNotEqual(Guid.Empty, id1);
        Assert.AreNotEqual(Guid.Empty, id2);
        Assert.AreNotEqual(id1, id2); // Each call returns unique ID
    }

    [TestMethod]
    public async Task CreateNewConversationAsync_DoesNotSaveImmediately()
    {
        // Act
        await _manager.CreateNewConversationAsync();

        // Assert
        _mockRepository.Verify(r => r.SaveAsync(It.IsAny<Conversation>()), Times.Never);
    }

    private AppConfiguration CreateTestAppConfig()
    {
        return new AppConfiguration
        {
            Agent = new AgentConfiguration
            {
                SystemPrompt = "Test prompt",
                ContextWindowSize = 50
            },
            LLM = new LLMConfiguration
            {
                Provider = "Anthropic",
                Temperature = 1.0,
                Anthropic = new AnthropicConfiguration
                {
                    Model = "claude-sonnet-4-5"
                }
            }
        };
    }
}
```

### Implementation Order (TDD)

1. **Create interface** `IConversationHistoryManager`
2. **Write tests for `SaveCurrentConversationAsync()`** (RED)
3. **Implement `SaveCurrentConversationAsync()`** (GREEN)
4. **Write tests for `LoadConversationAsync()`** (RED)
5. **Implement `LoadConversationAsync()`** (GREEN)
6. **Write tests for `CreateNewConversationAsync()`** (RED)
7. **Implement `CreateNewConversationAsync()`** (GREEN)
8. **Write remaining tests** (DeleteAsync, GetConversationListAsync) (RED)
9. **Implement remaining methods** (GREEN)
10. **Refactor** error handling and logging (REFACTOR)

### Exit Criteria

- ✅ ConversationHistoryManager fully implemented
- ✅ All orchestration logic tested
- ✅ All tests passing
- ✅ Error handling verified
- ✅ Code committed and pushed

**Estimated Complexity**: Medium (orchestration logic, mocking)

---

## Session 4: UI Layer - Conversation Selector Component

**Goal**: Implement Blazor component for conversation selection.

**Dependencies**: Sessions 1-3 (domain, repository, manager)

**Files to Create**:

1. `TransparentAiAgentGui/Components/ConversationHistory/ConversationSelector.razor`
2. `TransparentAiAgentGui/Components/ConversationHistory/ConversationSelector.razor.css`

**Files to Modify**:

1. `TransparentAiAgentGui/Components/Pages/Home.razor` - Add ConversationSelector between header and MessageList
2. `TransparentAiAgentGui/Services/ConversationUIService.cs` - Add LoadConversationAsync method

### Testing Strategy

**Note**: Blazor component testing is more complex. Options:

1. **Manual testing**: Run the application and test UI interactions manually
2. **bUnit**: Blazor component testing framework (would need to add dependency)
3. **Integration tests**: Test through ConversationUIService

**Recommendation**: Focus on **integration tests** for ConversationUIService + **manual testing** for UI.

For this session, TDD is applied to the **service layer changes** (ConversationUIService.LoadConversationAsync), not the Razor component itself.

### What to Test (Lean TDD)

**✅ Test These**:
- `ConversationUIService.LoadConversationAsync()` - New method
  - Clears current conversation
  - Loads messages from Conversation
  - Adds messages to ConversationManager
  - Raises MessagesChanged event
  - Updates conversation ID
- `ConversationUIService.CurrentConversationId` - Property exposure
  - Returns ConversationManager.ConversationId

**❌ Skip These**:
- Razor component rendering (manual testing)
- Dropdown UI behavior (manual testing)
- CSS styling (visual inspection)

### Test Examples

```csharp
[TestClass]
public class ConversationUIServiceTests
{
    // Existing tests for SendMessage, etc.

    [TestMethod]
    public async Task LoadConversationAsync_ClearsCurrentConversation()
    {
        // Arrange
        var service = CreateConversationUIService();
        var conversationManager = GetConversationManager(service);
        conversationManager.AddMessage(new UserMessage("Old message"));

        var newConversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            Messages = new List<IMessage>
            {
                new UserMessage("New message")
            }
        };

        // Act
        await service.LoadConversationAsync(newConversation);

        // Assert
        var messages = conversationManager.GetAllMessages();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("New message", messages[0].Content);
    }

    [TestMethod]
    public async Task LoadConversationAsync_RaisesMessagesChangedEvent()
    {
        // Arrange
        var service = CreateConversationUIService();
        var eventRaised = false;
        service.MessagesChanged += (sender, args) => eventRaised = true;

        var conversation = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            Messages = new List<IMessage>
            {
                new UserMessage("Test")
            }
        };

        // Act
        await service.LoadConversationAsync(conversation);

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void CurrentConversationId_ReturnsConversationManagerId()
    {
        // Arrange
        var service = CreateConversationUIService();
        var conversationManager = GetConversationManager(service);

        // Act
        var id = service.CurrentConversationId;

        // Assert
        Assert.AreEqual(conversationManager.ConversationId, id);
    }
}
```

### Implementation Order

1. **Write tests for ConversationUIService.LoadConversationAsync()** (RED)
2. **Implement LoadConversationAsync() in ConversationUIService** (GREEN)
3. **Add CurrentConversationId property** (GREEN)
4. **Create ConversationSelector.razor component** (Implementation based on design)
5. **Create ConversationSelector.razor.css** (Styling based on design)
6. **Modify Home.razor to include ConversationSelector** (Integration)
7. **Register dependencies in DI container** (Program.cs or Startup.cs)
8. **Manual testing**: Run application and verify UI behavior

### Manual Testing Checklist

When running the application:

- [ ] ConversationSelector appears between header and message list
- [ ] Dropdown shows current conversation as "(Current)"
- [ ] "+ New" button clears conversation
- [ ] Selecting conversation from dropdown loads it
- [ ] Conversation name updates after sending first message
- [ ] Loading indicator appears during operations
- [ ] Error messages display when operations fail
- [ ] Auto-save happens after each message (check ./conversations/ directory)

### Exit Criteria

- ✅ ConversationUIService.LoadConversationAsync() implemented and tested
- ✅ ConversationSelector component created
- ✅ Home.razor integration complete
- ✅ Dependencies registered in DI
- ✅ Manual testing completed successfully
- ✅ Code committed and pushed

**Estimated Complexity**: Medium (Blazor component + service integration)

---

## Session 5: Integration - Auto-Save & DI Wiring

**Goal**: Integrate auto-save functionality and wire up all dependencies.

**Dependencies**: Sessions 1-4 (all previous layers)

**Files to Modify**:

1. `TransparentAiAgentGui/Services/ConversationUIService.cs` - Add auto-save hooks
2. `TransparentAiAgentGui/Program.cs` (or DI configuration) - Register services
3. `TransparentAiAgentCore/Infrastructure/DependencyInjection.cs` (if exists) - Register repository

### What to Test (Lean TDD)

**✅ Test These**:
- `ConversationUIService.AutoSaveConversationAsync()` - New private method
  - Retrieves current configuration
  - Gets messages from ConversationManager
  - Calls ConversationHistoryManager.SaveCurrentConversationAsync()
  - Handles failures gracefully (logs, doesn't throw)
- Auto-save integration
  - Auto-save called after SendMessageStreamingAsync()
  - Auto-save called after SendMessageAsync()
  - Auto-save failures don't crash the app

**Test Strategy**: Use mocks for IConversationHistoryManager and IConfigurationService.

### Test Examples

```csharp
[TestClass]
public class ConversationUIServiceAutoSaveTests
{
    private Mock<IConversationHistoryManager> _mockHistoryManager;
    private Mock<IConfigurationService> _mockConfigService;
    private ConversationUIService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockHistoryManager = new Mock<IConversationHistoryManager>();
        _mockConfigService = new Mock<IConfigurationService>();

        _mockConfigService.Setup(c => c.GetConfiguration())
            .Returns(CreateTestAppConfig());

        // Create service with mocked dependencies
        _service = CreateServiceWithMocks(
            _mockHistoryManager.Object,
            _mockConfigService.Object
        );
    }

    [TestMethod]
    public async Task SendMessageStreamingAsync_CallsAutoSave()
    {
        // Arrange
        var userMessage = "Test message";

        // Act
        await _service.SendMessageStreamingAsync(userMessage);
        await Task.Delay(100); // Wait for fire-and-forget auto-save

        // Assert
        _mockHistoryManager.Verify(
            h => h.SaveCurrentConversationAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<IMessage>>(),
                It.IsAny<AppConfiguration>()
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task AutoSave_SaveFails_DoesNotThrow()
    {
        // Arrange
        _mockHistoryManager
            .Setup(h => h.SaveCurrentConversationAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<IMessage>>(),
                It.IsAny<AppConfiguration>()
            ))
            .ThrowsAsync(new IOException("Save failed"));

        var userMessage = "Test message";

        // Act & Assert - should not throw
        await _service.SendMessageStreamingAsync(userMessage);
        await Task.Delay(100); // Wait for auto-save to fail

        // No exception should be thrown - failure is logged
    }

    [TestMethod]
    public async Task AutoSave_PassesCurrentConfiguration()
    {
        // Arrange
        var expectedConfig = CreateTestAppConfig();
        _mockConfigService.Setup(c => c.GetConfiguration())
            .Returns(expectedConfig);

        var userMessage = "Test message";

        // Act
        await _service.SendMessageStreamingAsync(userMessage);
        await Task.Delay(100);

        // Assert
        _mockHistoryManager.Verify(
            h => h.SaveCurrentConversationAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<IMessage>>(),
                It.Is<AppConfiguration>(c => c == expectedConfig)
            ),
            Times.Once
        );
    }
}
```

### Implementation Order (TDD)

1. **Write tests for auto-save integration** (RED)
2. **Add IConversationHistoryManager and IConfigurationService to ConversationUIService constructor**
3. **Implement AutoSaveConversationAsync() private method** (GREEN)
4. **Add auto-save calls after RefreshMessages() in both streaming and non-streaming methods**
5. **Run tests and verify** (GREEN)
6. **Register IConversationRepository → JsonConversationRepository in DI**
7. **Register IConversationHistoryManager → ConversationHistoryManager in DI**
8. **Add IConfigurationService to ConversationUIService if not already present**
9. **Integration testing**: Run full application
10. **Verify auto-save works end-to-end**

### Dependency Registration Example

```csharp
// In Program.cs or Startup.cs

// Domain services (if any)

// Infrastructure services
builder.Services.AddSingleton<IMessageSerializer, MessageSerializer>();
builder.Services.AddSingleton<IConversationRepository, JsonConversationRepository>();

// Application services
builder.Services.AddScoped<IConversationHistoryManager, ConversationHistoryManager>();

// UI services (existing)
builder.Services.AddScoped<IConversationUIService, ConversationUIService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
```

### Integration Testing Checklist

Run the full application and verify:

- [ ] Application starts without errors
- [ ] Can send messages normally
- [ ] After each message, ./conversations/ directory has a file
- [ ] File contains correct JSON structure
- [ ] Configuration snapshot excludes API keys
- [ ] Conversation name is extracted from first user message
- [ ] Can load conversation from dropdown
- [ ] Loaded conversation shows all previous messages
- [ ] Can switch between conversations
- [ ] "+ New" button creates new conversation
- [ ] Auto-save failures (if simulated) don't crash app

### Exit Criteria

- ✅ Auto-save implemented and tested
- ✅ All dependencies registered in DI container
- ✅ Integration tests passing
- ✅ End-to-end manual testing successful
- ✅ No errors in application logs
- ✅ Code committed and pushed
- ✅ **FEATURE COMPLETE**

**Estimated Complexity**: Medium (integration, DI configuration, end-to-end testing)

---

## Session Summary

| Session | Focus | Files Created | Tests | Complexity |
|---------|-------|---------------|-------|------------|
| 1 | Domain entities | 4 classes | 2 test files | Low |
| 2 | JSON repository | 1 class | 1 test file | Medium |
| 3 | History manager | 2 classes | 1 test file | Medium |
| 4 | UI component | 2 components, service updates | Service tests + manual | Medium |
| 5 | Integration & auto-save | Service updates, DI config | Integration tests | Medium |

---

## Success Criteria (Overall)

After completing all 5 sessions:

- ✅ Users can view list of saved conversations
- ✅ Users can switch between conversations
- ✅ Users can create new conversations
- ✅ Conversations auto-save after every message
- ✅ Conversation names are extracted from first user message
- ✅ Configuration snapshots are stored (without API keys)
- ✅ All tests passing
- ✅ No errors or exceptions during normal use
- ✅ Code follows Clean Architecture principles
- ✅ Implementation follows Lean TDD guidelines

---

## Notes & Reminders

### Crash Recovery

If a session crashes:
1. Check last commit to see what was completed
2. Run tests to verify what's working
3. Continue from where the session left off
4. Don't skip RED phase - verify tests actually fail

### TDD Discipline

Throughout all sessions:
- ❌ **NEVER** skip the RED phase
- ❌ **NEVER** ignore unexpected test results
- ❌ **NEVER** test trivial compiler-enforced features
- ✅ **ALWAYS** verify test failures are for the RIGHT reason
- ✅ **ALWAYS** commit after each RED-GREEN-REFACTOR cycle
- ✅ **ALWAYS** run tests before committing

### Dependencies Between Sessions

Sessions are designed to be independent, but they build on each other:

```
Session 1 (Domain)
    ↓
Session 2 (Repository) ← depends on Session 1
    ↓
Session 3 (Manager) ← depends on Sessions 1 & 2
    ↓
Session 4 (UI) ← depends on Sessions 1-3
    ↓
Session 5 (Integration) ← depends on Sessions 1-4
```

**You can pause after any session** and pick up later. Just ensure each session's exit criteria are met before moving on.

---

**End of Implementation Plan**
