# Message Architecture Implementation Plan - Four-Tier Hierarchy

**Date**: 2025-11-13
**Architecture Document**: `MESSAGE_ARCHITECTURE_REDESIGN.md`
**TDD Approach**: Following `.claude/skills/tdd/SKILL.md` (Lean TDD)
**Status**: READY FOR IMPLEMENTATION

---

## Implementation Principles

### TDD Workflow (Red-Green-Refactor)

**RED Phase:**
- ✅ Test code compiles successfully
- ✅ Test runs and executes
- ✅ Test FAILS due to missing/incorrect implementation
- ✅ Failure message shows exactly what's missing
- ❌ NOT a compilation error

**GREEN Phase:**
- ✅ Write MINIMUM code to make test pass
- ✅ No extra features
- ✅ Test MUST pass

**REFACTOR Phase:**
- ✅ Improve code quality while keeping tests green
- ✅ Run tests after each change

### Session Independence

Each session:
- Can be completed independently
- Has clear entry/exit criteria
- Takes ~1-2 hours of focused work
- Leaves codebase in compilable, testable state
- Has rollback strategy if needed

### Exception: Refactoring Existing Code

When refactoring existing code with valid logic:
- ✅ It's OK if test passes immediately (as long as test exists)
- ✅ Don't remove working logic just to make test fail
- ✅ Focus on ensuring test coverage exists

---

## Phase 0: Baseline & Prerequisites

### Session 0.1: Verify Current State

**Objective**: Ensure clean baseline before starting refactor

**Prerequisites**: None

**Tasks**:
1. Run all existing tests
2. Document any failing tests
3. Fix any existing test failures
4. Document baseline state

**Verification**:
```bash
dotnet test TransparentAiAgentCore_Tests
# Expected: All tests pass
```

**Exit Criteria**:
- ✅ All existing tests pass (100%)
- ✅ Baseline documented

**Rollback**: N/A (baseline)

---

## Phase 1: Core Message Hierarchy (Domain Layer)

### Session 1.1: Add MessageTypeDiscriminator to IMessage

**Objective**: Add discriminator property to support serialization

**Prerequisites**:
- Session 0.1 complete

**TDD Steps**:

1. **RED: Write failing test**
   ```csharp
   [TestClass]
   public class MessageTypeDiscriminatorTests
   {
       [TestMethod]
       public void UserMessage_HasDiscriminator()
       {
           var msg = new UserMessage("Test");
           Assert.IsFalse(string.IsNullOrEmpty(msg.MessageTypeDiscriminator));
       }
   }
   ```
   - Add minimal stub: `string MessageTypeDiscriminator { get; }` to IMessage
   - Run test → Should FAIL (existing messages don't have discriminator)

2. **GREEN: Add discriminator to existing messages**
   - Add discriminator property to UserMessage: `"User"` (temporary)
   - Run test → Should PASS

3. **REFACTOR: Update IMessage interface**
   - Add XML documentation
   - Ensure all existing IMessage implementations compile

**Files Changed**:
- `TransparentAiAgentCore/Domain/Models/IMessage.cs`
- `TransparentAiAgentCore/Domain/Models/UserMessage.cs`
- `TransparentAiAgentCore/Domain/Models/AssistantMessage.cs`
- `TransparentAiAgentCore/Domain/Models/SystemMessage.cs`
- `TransparentAiAgentCore/Domain/Models/ToolResultMessage.cs`
- `TransparentAiAgentCore/Domain/Models/AssistantToolCallMessage.cs`
- `TransparentAiAgentCore_Tests/Domain/Models/MessageTypeDiscriminatorTests.cs` (NEW)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~MessageTypeDiscriminatorTests"
# Expected: All tests pass
dotnet build
# Expected: Clean build
```

**Exit Criteria**:
- ✅ IMessage has MessageTypeDiscriminator property
- ✅ All existing messages have temporary discriminator
- ✅ All tests pass
- ✅ Code compiles

**Rollback**: Revert IMessage changes, remove test file

---

### Session 1.2: Create Abstract Base Classes (UserMessage, LlmMessage, ApplicationMessage, ToolMessage)

**Objective**: Create 4 abstract base classes for message origin categories

**Prerequisites**:
- Session 1.1 complete

**TDD Steps**:

1. **Setup: Rename existing UserMessage**
   - Rename `UserMessage.cs` to `DirectUserMessage.cs`
   - Keep as concrete class temporarily
   - Update all references
   - Ensure all tests still pass

2. **RED: Write tests for abstract UserMessage**
   ```csharp
   [TestClass]
   public class DirectUserMessageTests
   {
       [TestMethod]
       public void Constructor_ValidContent_SetsDiscriminator()
       {
           var msg = new DirectUserMessage("Test");
           Assert.AreEqual("User.Direct", msg.MessageTypeDiscriminator);
       }

       [TestMethod]
       public void Role_IsUser()
       {
           var msg = new DirectUserMessage("Test");
           Assert.AreEqual(MessageRole.User, msg.Role);
       }
   }
   ```
   - Create abstract `UserMessage : IMessage`
   - Make `DirectUserMessage : UserMessage`
   - Add minimal stubs (no logic)
   - Run tests → Should FAIL (discriminator not set correctly)

3. **GREEN: Implement UserMessage hierarchy**
   - Implement properties in abstract UserMessage
   - Implement discriminator in DirectUserMessage
   - Run tests → Should PASS

4. **RED: Write tests for LlmMessage, ApplicationMessage, ToolMessage**
   - Create test files for each base class
   - Tests verify Role and common properties
   - Add minimal abstract class stubs
   - Run tests → Should FAIL

5. **GREEN: Implement remaining base classes**
   - Implement abstract classes with properties
   - Run tests → Should PASS

6. **REFACTOR: Clean up and document**
   - Add XML documentation
   - Add RULE comments for boundaries
   - Ensure consistent patterns

**Files Changed**:
- `TransparentAiAgentCore/Domain/Models/UserMessage.cs` (CREATED - abstract)
- `TransparentAiAgentCore/Domain/Models/DirectUserMessage.cs` (RENAMED from UserMessage)
- `TransparentAiAgentCore/Domain/Models/LlmMessage.cs` (CREATED - abstract)
- `TransparentAiAgentCore/Domain/Models/ApplicationMessage.cs` (CREATED - abstract)
- `TransparentAiAgentCore/Domain/Models/ToolMessage.cs` (CREATED - abstract)
- `TransparentAiAgentCore_Tests/Domain/Models/DirectUserMessageTests.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/UserMessageBaseTests.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/LlmMessageBaseTests.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/ApplicationMessageBaseTests.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/ToolMessageBaseTests.cs` (CREATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~Domain.Models"
# Expected: All tests pass
dotnet build
# Expected: Clean build
```

**Exit Criteria**:
- ✅ 4 abstract base classes created
- ✅ DirectUserMessage inherits from UserMessage
- ✅ Tests verify Role and discriminator
- ✅ All existing tests still pass
- ✅ Code compiles

**Rollback**: Revert to single concrete UserMessage, delete abstract classes

---

### Session 1.3: Create LlmTextMessage and LlmToolCallMessage

**Objective**: Refactor existing AssistantMessage and AssistantToolCallMessage to new hierarchy

**Prerequisites**:
- Session 1.2 complete

**TDD Steps**:

1. **RED: Write tests for LlmTextMessage**
   ```csharp
   [TestClass]
   public class LlmTextMessageTests
   {
       [TestMethod]
       public void Constructor_ValidContent_SetsDiscriminator()
       {
           var msg = new LlmTextMessage("Response");
           Assert.AreEqual("Llm.Text", msg.MessageTypeDiscriminator);
       }

       [TestMethod]
       public void Role_IsAssistant()
       {
           var msg = new LlmTextMessage("Response");
           Assert.AreEqual(MessageRole.Assistant, msg.Role);
       }

       [TestMethod]
       public void Constructor_EmptyContent_IsValid()
       {
           // Empty content is valid for tool-call-only responses
           var msg = new LlmTextMessage("");
           Assert.AreEqual("", msg.Content);
       }
   }
   ```
   - Create `LlmTextMessage : LlmMessage` stub (no logic)
   - Run tests → Should FAIL (properties not set)

2. **GREEN: Implement LlmTextMessage**
   - Copy logic from existing AssistantMessage
   - Implement constructor and properties
   - Run tests → Should PASS

3. **RED: Write tests for LlmToolCallMessage**
   ```csharp
   [TestClass]
   public class LlmToolCallMessageTests
   {
       [TestMethod]
       public void Constructor_ValidToolCalls_SetsProperties()
       {
           var toolCalls = new List<ToolCall>
           {
               new ToolCall("id1", "tool1", "{}")
           };
           var msg = new LlmToolCallMessage("", toolCalls);

           Assert.AreEqual("Llm.ToolCall", msg.MessageTypeDiscriminator);
           Assert.AreEqual(1, msg.ToolCalls.Count);
       }

       [TestMethod]
       public void Constructor_NullToolCalls_ThrowsArgumentNullException()
       {
           Assert.ThrowsException<ArgumentNullException>(
               () => new LlmToolCallMessage("", null!));
       }
   }
   ```
   - Create `LlmToolCallMessage : LlmMessage` stub
   - Run tests → Should FAIL

4. **GREEN: Implement LlmToolCallMessage**
   - Copy logic from existing AssistantToolCallMessage
   - Implement validation and properties
   - Run tests → Should PASS

5. **REFACTOR: Update all usages**
   - Keep old AssistantMessage/AssistantToolCallMessage temporarily
   - Add TODO comments for migration
   - Ensure both old and new work side-by-side

**Files Changed**:
- `TransparentAiAgentCore/Domain/Models/LlmTextMessage.cs` (CREATED)
- `TransparentAiAgentCore/Domain/Models/LlmToolCallMessage.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/LlmTextMessageTests.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/LlmToolCallMessageTests.cs` (CREATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~LlmTextMessageTests"
dotnet test --filter "FullyQualifiedName~LlmToolCallMessageTests"
# Expected: All new tests pass
dotnet test
# Expected: All existing tests still pass
```

**Exit Criteria**:
- ✅ LlmTextMessage created and tested
- ✅ LlmToolCallMessage created and tested
- ✅ Validation logic works
- ✅ All tests pass
- ✅ Old message types still work (parallel existence)

**Rollback**: Delete new LlmMessage subtypes

---

### Session 1.4: Create ScenarioUserMessage and ScenarioAssistantMessage

**Objective**: Create application-originated message types for scenarios

**Prerequisites**:
- Session 1.2 complete

**TDD Steps**:

1. **RED: Write tests for ScenarioUserMessage**
   ```csharp
   [TestClass]
   public class ScenarioUserMessageTests
   {
       [TestMethod]
       public void Constructor_WithAnnotation_SetsProperties()
       {
           var msg = new ScenarioUserMessage("Hello", "Teaching step 1");

           Assert.AreEqual("Hello", msg.Content);
           Assert.AreEqual("Teaching step 1", msg.Annotation);
           Assert.AreEqual(MessageRole.User, msg.Role);
           Assert.AreEqual("Application.ScenarioUser", msg.MessageTypeDiscriminator);
           Assert.AreEqual("scenario_user_message", msg.MessageType);
       }

       [TestMethod]
       public void Constructor_WithoutAnnotation_AnnotationIsNull()
       {
           var msg = new ScenarioUserMessage("Hello");
           Assert.IsNull(msg.Annotation);
       }

       [TestMethod]
       public void Constructor_NullContent_ThrowsArgumentException()
       {
           Assert.ThrowsException<ArgumentException>(
               () => new ScenarioUserMessage(null!));
       }
   }
   ```
   - Create `ScenarioUserMessage : ApplicationMessage` stub
   - Run tests → Should FAIL

2. **GREEN: Implement ScenarioUserMessage**
   - Implement constructor with validation
   - Implement properties (Annotation, MessageType)
   - Set discriminator correctly
   - Run tests → Should PASS

3. **RED: Write tests for ScenarioAssistantMessage**
   ```csharp
   [TestClass]
   public class ScenarioAssistantMessageTests
   {
       [TestMethod]
       public void Constructor_WithAnnotation_SetsProperties()
       {
           var msg = new ScenarioAssistantMessage("Response", "Teaching response");

           Assert.AreEqual("Response", msg.Content);
           Assert.AreEqual("Teaching response", msg.Annotation);
           Assert.AreEqual(MessageRole.Assistant, msg.Role);
           Assert.AreEqual("Application.ScenarioAssistant", msg.MessageTypeDiscriminator);
       }

       [TestMethod]
       public void Role_IsAssistant()
       {
           var msg = new ScenarioAssistantMessage("Response");
           Assert.AreEqual(MessageRole.Assistant, msg.Role);
       }
   }
   ```
   - Create `ScenarioAssistantMessage : ApplicationMessage` stub
   - Run tests → Should FAIL

4. **GREEN: Implement ScenarioAssistantMessage**
   - Implement constructor
   - Implement properties
   - Run tests → Should PASS

5. **REFACTOR: Ensure consistency**
   - Verify both have consistent patterns
   - Add XML documentation
   - Verify MessageType values match

**Files Changed**:
- `TransparentAiAgentCore/Domain/Models/ScenarioUserMessage.cs` (CREATED)
- `TransparentAiAgentCore/Domain/Models/ScenarioAssistantMessage.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/ScenarioUserMessageTests.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/ScenarioAssistantMessageTests.cs` (CREATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~ScenarioUserMessageTests"
dotnet test --filter "FullyQualifiedName~ScenarioAssistantMessageTests"
# Expected: All new tests pass
```

**Exit Criteria**:
- ✅ ScenarioUserMessage created and tested
- ✅ ScenarioAssistantMessage created and tested
- ✅ Annotation property works
- ✅ MessageType set correctly
- ✅ All tests pass

**Rollback**: Delete scenario message types

---

### Session 1.5: Create ToolResultMessage and ToolErrorMessage

**Objective**: Refactor existing ToolResultMessage and create ToolErrorMessage

**Prerequisites**:
- Session 1.2 complete

**TDD Steps**:

1. **RED: Write tests for new ToolResultMessage**
   ```csharp
   [TestClass]
   public class ToolResultMessageTests
   {
       [TestMethod]
       public void Constructor_ValidParameters_SetsProperties()
       {
           var toolCallId = Guid.NewGuid();
           var msg = new ToolResultMessage(toolCallId, "get_weather", "{\"temp\":20}", false);

           Assert.AreEqual(toolCallId, msg.ToolCallId);
           Assert.AreEqual("get_weather", msg.ToolName);
           Assert.AreEqual("{\"temp\":20}", msg.Result);
           Assert.IsFalse(msg.IsError);
           Assert.AreEqual(MessageRole.Tool, msg.Role);
           Assert.AreEqual("Tool.Result", msg.MessageTypeDiscriminator);
       }

       [TestMethod]
       public void Constructor_NullToolName_ThrowsArgumentNullException()
       {
           Assert.ThrowsException<ArgumentNullException>(
               () => new ToolResultMessage(Guid.NewGuid(), null!, "result", false));
       }

       [TestMethod]
       public void Constructor_NullResult_ThrowsArgumentNullException()
       {
           Assert.ThrowsException<ArgumentNullException>(
               () => new ToolResultMessage(Guid.NewGuid(), "tool", null!, false));
       }
   }
   ```
   - Note: Existing ToolResultMessage tests exist, need to update
   - Create NEW ToolResultMessage inheriting from ToolMessage
   - Temporarily rename old one to ToolResultMessageOld
   - Run tests → Should FAIL (new implementation missing)

2. **GREEN: Implement new ToolResultMessage**
   - Implement constructor with validation
   - Implement properties
   - Set discriminator
   - Run tests → Should PASS

3. **RED: Write tests for ToolErrorMessage**
   ```csharp
   [TestClass]
   public class ToolErrorMessageTests
   {
       [TestMethod]
       public void Constructor_ValidParameters_SetsProperties()
       {
           var toolCallId = Guid.NewGuid();
           var exception = new InvalidOperationException("Test error");
           var msg = new ToolErrorMessage(toolCallId, "tool", "Error message", exception);

           Assert.AreEqual(toolCallId, msg.ToolCallId);
           Assert.AreEqual("tool", msg.ToolName);
           Assert.AreEqual("Error message", msg.ErrorMessage);
           Assert.AreEqual(exception, msg.Exception);
           Assert.AreEqual("Tool.Error", msg.MessageTypeDiscriminator);
           Assert.IsTrue(msg.Content.Contains("Error executing tool"));
       }

       [TestMethod]
       public void Constructor_NullErrorMessage_ThrowsArgumentNullException()
       {
           Assert.ThrowsException<ArgumentNullException>(
               () => new ToolErrorMessage(Guid.NewGuid(), "tool", null!));
       }

       [TestMethod]
       public void Constructor_WithoutException_CreatesMessage()
       {
           var msg = new ToolErrorMessage(Guid.NewGuid(), "tool", "Error");
           Assert.IsNull(msg.Exception);
       }
   }
   ```
   - Create `ToolErrorMessage : ToolMessage` stub
   - Run tests → Should FAIL

4. **GREEN: Implement ToolErrorMessage**
   - Implement constructor with validation
   - Implement error message formatting
   - Run tests → Should PASS

5. **REFACTOR: Update old ToolResultMessage usages**
   - Keep old ToolResultMessageOld temporarily
   - Document migration path
   - Ensure both versions work

**Files Changed**:
- `TransparentAiAgentCore/Domain/Models/ToolResultMessage.cs` (UPDATED - now inherits ToolMessage)
- `TransparentAiAgentCore/Domain/Models/ToolResultMessageOld.cs` (TEMPORARY - old version)
- `TransparentAiAgentCore/Domain/Models/ToolErrorMessage.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Domain/Models/ToolResultMessageTests.cs` (UPDATED)
- `TransparentAiAgentCore_Tests/Domain/Models/ToolErrorMessageTests.cs` (CREATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~ToolResultMessageTests"
dotnet test --filter "FullyQualifiedName~ToolErrorMessageTests"
# Expected: All tests pass
```

**Exit Criteria**:
- ✅ New ToolResultMessage inherits from ToolMessage
- ✅ ToolErrorMessage created and tested
- ✅ Validation works
- ✅ All tests pass
- ✅ Old version preserved temporarily

**Rollback**: Restore old ToolResultMessage, delete ToolErrorMessage

---

## Phase 2: Message Pipeline Updates

### Session 2.1: Update MessagePipeline.ConvertToLLMMessage for New Types

**Objective**: Add conversion logic for all new message types

**Prerequisites**:
- Sessions 1.3, 1.4, 1.5 complete (all new types created)

**TDD Steps**:

1. **RED: Write tests for DirectUserMessage conversion**
   ```csharp
   [TestClass]
   public class MessagePipelineConversionTests
   {
       private IMessagePipeline _pipeline;

       [TestInitialize]
       public void Setup()
       {
           _pipeline = new MessagePipeline();
       }

       [TestMethod]
       public void ConvertToLLMMessage_DirectUserMessage_ConvertsToUserRole()
       {
           var domainMsg = new DirectUserMessage("Hello");
           var llmMsg = _pipeline.ConvertToLLMMessage(domainMsg);

           Assert.AreEqual("user", llmMsg.Role);
           Assert.AreEqual("Hello", llmMsg.Content);
       }
   }
   ```
   - Update MessagePipeline.ConvertToLLMMessage switch statement (add case, no logic)
   - Run test → Should FAIL (conversion not implemented)

2. **GREEN: Implement DirectUserMessage conversion**
   - Add conversion logic (same as old UserMessage)
   - Run test → Should PASS

3. **RED: Write tests for LlmTextMessage conversion**
   ```csharp
   [TestMethod]
   public void ConvertToLLMMessage_LlmTextMessage_ConvertsToAssistantRole()
   {
       var domainMsg = new LlmTextMessage("Response");
       var llmMsg = _pipeline.ConvertToLLMMessage(domainMsg);

       Assert.AreEqual("assistant", llmMsg.Role);
       Assert.AreEqual("Response", llmMsg.Content);
   }

   [TestMethod]
   public void ConvertToLLMMessage_LlmTextMessage_EmptyContent_IsValid()
   {
       var domainMsg = new LlmTextMessage("");
       var llmMsg = _pipeline.ConvertToLLMMessage(domainMsg);

       Assert.AreEqual("assistant", llmMsg.Role);
       Assert.AreEqual("", llmMsg.Content);
   }
   ```
   - Add case for LlmTextMessage (no logic)
   - Run tests → Should FAIL

4. **GREEN: Implement LlmTextMessage conversion**
   - Add conversion logic
   - Run tests → Should PASS

5. **RED: Write tests for ScenarioUserMessage conversion**
   ```csharp
   [TestMethod]
   public void ConvertToLLMMessage_ScenarioUserMessage_ConvertsToUserRole_StripsAnnotation()
   {
       var domainMsg = new ScenarioUserMessage("Hello", "This annotation should be stripped");
       var llmMsg = _pipeline.ConvertToLLMMessage(domainMsg);

       Assert.AreEqual("user", llmMsg.Role);
       Assert.AreEqual("Hello", llmMsg.Content);
       Assert.IsFalse(llmMsg.Content.Contains("annotation"));
   }
   ```
   - Add case for ScenarioUserMessage (no logic)
   - Run test → Should FAIL

6. **GREEN: Implement ScenarioUserMessage conversion**
   - Add conversion logic (strips annotation)
   - Run test → Should PASS

7. **RED: Write tests for ToolResultMessage conversion**
   ```csharp
   [TestMethod]
   public void ConvertToLLMMessage_ToolResultMessage_ConvertsToToolRole()
   {
       var toolCallId = Guid.NewGuid();
       var domainMsg = new ToolResultMessage(toolCallId, "get_weather", "{\"temp\":20}", false);
       var llmMsg = _pipeline.ConvertToLLMMessage(domainMsg);

       Assert.AreEqual("tool", llmMsg.Role);
       Assert.AreEqual("{\"temp\":20}", llmMsg.Content);
       Assert.IsNotNull(llmMsg.ToolCallId);
   }
   ```
   - Add case for ToolResultMessage (no logic)
   - Run test → Should FAIL

8. **GREEN: Implement ToolResultMessage conversion**
   - Add conversion logic
   - Run test → Should PASS

9. **REFACTOR: Handle all new types**
   - Add cases for LlmToolCallMessage, ScenarioAssistantMessage, ToolErrorMessage
   - Add comprehensive tests for mixed message types
   - Ensure old message types still work

**Files Changed**:
- `TransparentAiAgentCore/Application/Pipeline/MessagePipeline.cs` (UPDATED)
- `TransparentAiAgentCore_Tests/Application/Pipeline/MessagePipelineConversionTests.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Application/Pipeline/MessagePipelineTests.cs` (UPDATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~MessagePipelineConversionTests"
dotnet test --filter "FullyQualifiedName~MessagePipelineTests"
# Expected: All tests pass
```

**Exit Criteria**:
- ✅ All new message types have conversion logic
- ✅ Annotation stripping works for scenarios
- ✅ All tests pass
- ✅ Old and new types both convert correctly

**Rollback**: Revert MessagePipeline.cs changes

---

## Phase 3: Orchestrator API Refactor

### Session 3.1: Add ProcessApplicationMessageAsync Method

**Objective**: Add new orchestrator method for application-originated messages

**Prerequisites**:
- Session 1.4 complete (ScenarioUserMessage exists)
- Session 2.1 complete (Pipeline converts ScenarioUserMessage)

**TDD Steps**:

1. **RED: Write tests for ProcessApplicationMessageAsync**
   ```csharp
   [TestClass]
   public class AgentOrchestratorApplicationMessageTests
   {
       private IAgentOrchestrator _orchestrator;
       private MockLLMProvider _mockLLMProvider;
       private IConversationManager _conversationManager;

       [TestInitialize]
       public void Setup()
       {
           _mockLLMProvider = new MockLLMProvider();
           _conversationManager = new ConversationManager(10, new TransparencyService());
           // ... setup orchestrator
       }

       [TestMethod]
       public async Task ProcessApplicationMessageAsync_ScenarioUserMessage_AddsToConversation()
       {
           _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
           var msg = new ScenarioUserMessage("Test", "Annotation");

           await ConsumeStreamAsync(_orchestrator.ProcessApplicationMessageAsync(msg));

           var messages = _conversationManager.GetAllMessages();
           Assert.IsTrue(messages.Any(m => m is ScenarioUserMessage));
       }

       [TestMethod]
       public async Task ProcessApplicationMessageAsync_ScenarioUserMessage_CallsLLM()
       {
           _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
           var msg = new ScenarioUserMessage("Test");

           await ConsumeStreamAsync(_orchestrator.ProcessApplicationMessageAsync(msg));

           Assert.IsTrue(_mockLLMProvider.WasSendRequestCalled);
       }

       [TestMethod]
       public async Task ProcessApplicationMessageAsync_ScenarioAssistantMessage_DoesNotCallLLM()
       {
           var msg = new ScenarioAssistantMessage("Response", "Teaching");

           var chunks = await _orchestrator.ProcessApplicationMessageAsync(msg).ToListAsync();

           Assert.IsFalse(_mockLLMProvider.WasSendRequestCalled);
       }
   }
   ```
   - Add method signature to IAgentOrchestrator (no implementation)
   - Add stub implementation to AgentOrchestrator (return empty stream)
   - Run tests → Should FAIL (messages not added, LLM not called)

2. **GREEN: Implement ProcessApplicationMessageAsync**
   - Add message to conversation
   - Implement routing logic (switch on ApplicationMessage subtypes)
   - Handle ScenarioUserMessage → trigger LLM
   - Handle ScenarioAssistantMessage → don't trigger LLM
   - Run tests → Should PASS

3. **REFACTOR: Add logging and error handling**
   - Add transparency events
   - Handle cancellation
   - Add error handling

**Files Changed**:
- `TransparentAiAgentCore/Application/Agent/IAgentOrchestrator.cs` (UPDATED)
- `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs` (UPDATED)
- `TransparentAiAgentCore_Tests/Application/Agent/AgentOrchestratorApplicationMessageTests.cs` (CREATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AgentOrchestratorApplicationMessageTests"
# Expected: All tests pass
dotnet test --filter "FullyQualifiedName~AgentOrchestratorTests"
# Expected: All existing tests still pass
```

**Exit Criteria**:
- ✅ ProcessApplicationMessageAsync method exists
- ✅ ScenarioUserMessage triggers LLM
- ✅ ScenarioAssistantMessage doesn't trigger LLM
- ✅ Messages added to conversation
- ✅ All tests pass

**Rollback**: Remove ProcessApplicationMessageAsync method and tests

---

### Session 3.2: Add ProcessToolMessageAsync Method

**Objective**: Add orchestrator method for tool-originated messages

**Prerequisites**:
- Session 1.5 complete (ToolResultMessage, ToolErrorMessage exist)
- Session 2.1 complete (Pipeline converts tool messages)

**TDD Steps**:

1. **RED: Write tests for ProcessToolMessageAsync**
   ```csharp
   [TestClass]
   public class AgentOrchestratorToolMessageTests
   {
       // Similar setup as Session 3.1

       [TestMethod]
       public async Task ProcessToolMessageAsync_ToolResultMessage_AddsToConversation()
       {
           _mockLLMProvider.SetNextResponse(new LLMResponse("Processed"));
           var msg = new ToolResultMessage(Guid.NewGuid(), "tool", "result", false);

           await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(msg));

           var messages = _conversationManager.GetAllMessages();
           Assert.IsTrue(messages.Any(m => m is ToolResultMessage));
       }

       [TestMethod]
       public async Task ProcessToolMessageAsync_ToolResultMessage_CallsLLM()
       {
           _mockLLMProvider.SetNextResponse(new LLMResponse("Processed"));
           var msg = new ToolResultMessage(Guid.NewGuid(), "tool", "result", false);

           await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(msg));

           Assert.IsTrue(_mockLLMProvider.WasSendRequestCalled);
       }

       [TestMethod]
       public async Task ProcessToolMessageAsync_ToolErrorMessage_AddsToConversation()
       {
           _mockLLMProvider.SetNextResponse(new LLMResponse("Handled error"));
           var msg = new ToolErrorMessage(Guid.NewGuid(), "tool", "Error message");

           await ConsumeStreamAsync(_orchestrator.ProcessToolMessageAsync(msg));

           var messages = _conversationManager.GetAllMessages();
           Assert.IsTrue(messages.Any(m => m is ToolErrorMessage));
       }
   }
   ```
   - Add method signature to IAgentOrchestrator
   - Add stub implementation
   - Run tests → Should FAIL

2. **GREEN: Implement ProcessToolMessageAsync**
   - Add message to conversation
   - Trigger LLM processing
   - Handle tool result and error messages
   - Run tests → Should PASS

3. **REFACTOR: Add transparency and error handling**

**Files Changed**:
- `TransparentAiAgentCore/Application/Agent/IAgentOrchestrator.cs` (UPDATED)
- `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs` (UPDATED)
- `TransparentAiAgentCore_Tests/Application/Agent/AgentOrchestratorToolMessageTests.cs` (CREATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AgentOrchestratorToolMessageTests"
# Expected: All tests pass
```

**Exit Criteria**:
- ✅ ProcessToolMessageAsync method exists
- ✅ Tool messages trigger LLM
- ✅ All tests pass

**Rollback**: Remove ProcessToolMessageAsync method and tests

---

### Session 3.3: Update ProcessUserMessageAsync to Accept DirectUserMessage

**Objective**: Refactor existing method to accept new DirectUserMessage type

**Prerequisites**:
- Session 1.3 complete (DirectUserMessage exists)

**TDD Steps**:

1. **Note: This is refactoring existing code with valid logic**
   - Exception applies: Tests can pass immediately
   - Focus on ensuring test coverage exists

2. **RED: Update existing tests to use DirectUserMessage**
   ```csharp
   [TestMethod]
   public async Task ProcessUserMessageAsync_DirectUserMessage_AddsToConversation()
   {
       _mockLLMProvider.SetNextResponse(new LLMResponse("Response"));
       var msg = new DirectUserMessage("Hello");  // Changed from old UserMessage

       await _orchestrator.ProcessUserMessageAsync(msg);

       var messages = _conversationManager.GetAllMessages();
       Assert.IsTrue(messages.Any(m => m is DirectUserMessage));
   }
   ```
   - Update method signature: `ProcessUserMessageAsync(UserMessage message)` (accepts abstract base)
   - Update existing tests to create DirectUserMessage
   - Run tests → Should PASS (logic already exists)

3. **REFACTOR: Clean up and document**
   - Add XML documentation explaining UserMessage is abstract
   - Verify DirectUserMessage is the only concrete UserMessage type currently

**Files Changed**:
- `TransparentAiAgentCore/Application/Agent/IAgentOrchestrator.cs` (UPDATED signature)
- `TransparentAiAgentCore/Application/Agent/AgentOrchestrator.cs` (UPDATED signature)
- `TransparentAiAgentCore_Tests/Application/Agent/AgentOrchestratorTests.cs` (UPDATED tests)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~AgentOrchestratorTests"
# Expected: All tests pass (immediately, because logic exists)
```

**Exit Criteria**:
- ✅ Method accepts UserMessage (abstract base)
- ✅ Tests use DirectUserMessage
- ✅ All existing tests pass
- ✅ Logic unchanged

**Rollback**: Revert signature changes

---

## Phase 4: Serialization Implementation

### Session 4.1: Create MessageSerializer with Type Registry

**Objective**: Implement discriminator-based serialization

**Prerequisites**:
- All message types created (Phase 1 complete)

**TDD Steps**:

1. **RED: Write tests for MessageSerializer.Serialize**
   ```csharp
   [TestClass]
   public class MessageSerializationTests
   {
       private MessageSerializer _serializer;

       [TestInitialize]
       public void Setup()
       {
           _serializer = new MessageSerializer();
       }

       [TestMethod]
       public void Serialize_DirectUserMessage_IncludesDiscriminator()
       {
           var msg = new DirectUserMessage("Hello");
           var json = _serializer.Serialize(msg);

           Assert.IsTrue(json.Contains("\"MessageTypeDiscriminator\":\"User.Direct\""));
           Assert.IsTrue(json.Contains("\"Content\":\"Hello\""));
       }

       [TestMethod]
       public void Serialize_ScenarioUserMessage_IncludesAnnotation()
       {
           var msg = new ScenarioUserMessage("Hello", "Teaching");
           var json = _serializer.Serialize(msg);

           Assert.IsTrue(json.Contains("\"Annotation\":\"Teaching\""));
       }
   }
   ```
   - Create `MessageSerializer` class stub (no implementation)
   - Add `Serialize(IMessage)` method returning empty string
   - Run tests → Should FAIL

2. **GREEN: Implement Serialize method**
   - Use JsonSerializer.Serialize with concrete type
   - Run tests → Should PASS

3. **RED: Write tests for Deserialize**
   ```csharp
   [TestMethod]
   public void Deserialize_DirectUserMessage_RestoresCorrectType()
   {
       var msg = new DirectUserMessage("Hello");
       var json = _serializer.Serialize(msg);

       var deserialized = _serializer.Deserialize(json);

       Assert.IsInstanceOfType(deserialized, typeof(DirectUserMessage));
       Assert.AreEqual("Hello", deserialized.Content);
   }

   [TestMethod]
   public void Deserialize_UnknownDiscriminator_ThrowsInvalidOperationException()
   {
       var json = "{\"MessageTypeDiscriminator\":\"Unknown.Type\"}";

       Assert.ThrowsException<InvalidOperationException>(
           () => _serializer.Deserialize(json));
   }
   ```
   - Add `Deserialize(string)` method stub (return null)
   - Run tests → Should FAIL

4. **GREEN: Implement Deserialize with type registry**
   - Create type registry dictionary
   - Parse JSON to read discriminator
   - Lookup type in registry
   - Deserialize to concrete type
   - Run tests → Should PASS

5. **RED: Write tests for round-trip with all message types**
   ```csharp
   [TestMethod]
   public void RoundTrip_AllMessageTypes_PreservesData()
   {
       var messages = new List<IMessage>
       {
           new DirectUserMessage("User"),
           new LlmTextMessage("LLM"),
           new ScenarioUserMessage("Scenario", "Teaching"),
           new ToolResultMessage(Guid.NewGuid(), "tool", "result", false)
       };

       foreach (var original in messages)
       {
           var json = _serializer.Serialize(original);
           var deserialized = _serializer.Deserialize(json);

           Assert.AreEqual(original.GetType(), deserialized.GetType());
           Assert.AreEqual(original.Content, deserialized.Content);
           Assert.AreEqual(original.MessageTypeDiscriminator, deserialized.MessageTypeDiscriminator);
       }
   }
   ```
   - Run test → Should FAIL (not all types in registry yet)

6. **GREEN: Add all types to registry**
   - Add all message types to type registry
   - Run test → Should PASS

7. **REFACTOR: Extract type registry to separate class**

**Files Changed**:
- `TransparentAiAgentCore/Infrastructure/Serialization/MessageSerializer.cs` (CREATED)
- `TransparentAiAgentCore/Infrastructure/Serialization/MessageTypeRegistry.cs` (CREATED)
- `TransparentAiAgentCore_Tests/Infrastructure/Serialization/MessageSerializationTests.cs` (CREATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~MessageSerializationTests"
# Expected: All tests pass
```

**Exit Criteria**:
- ✅ MessageSerializer.Serialize works
- ✅ MessageSerializer.Deserialize works
- ✅ Type registry contains all message types
- ✅ Round-trip preserves all data
- ✅ All tests pass

**Rollback**: Delete MessageSerializer and tests

---

## Phase 5: UI Layer Updates

### Session 5.1: Update UIMessage.FromDomainMessage for New Types

**Objective**: Add UI conversion logic for new message types

**Prerequisites**:
- All message types created (Phase 1 complete)

**TDD Steps**:

1. **RED: Write tests for DirectUserMessage UI conversion**
   ```csharp
   [TestClass]
   public class UIMessageConversionTests
   {
       [TestMethod]
       public void FromDomainMessage_DirectUserMessage_SetsStandardUserDisplay()
       {
           var domainMsg = new DirectUserMessage("Hello");
           var uiMsg = UIMessage.FromDomainMessage(domainMsg);

           Assert.AreEqual("Hello", uiMsg.Content);
           Assert.IsFalse(uiMsg.IsAutoMessage);
           Assert.IsNull(uiMsg.Annotation);
           Assert.AreEqual("message-user", uiMsg.CssClass);
       }
   }
   ```
   - Update UIMessage.FromDomainMessage to handle DirectUserMessage (no logic)
   - Run test → Should FAIL

2. **GREEN: Implement DirectUserMessage conversion**
   - Add case for DirectUserMessage
   - Set properties correctly
   - Run test → Should PASS

3. **RED: Write tests for ScenarioUserMessage UI conversion**
   ```csharp
   [TestMethod]
   public void FromDomainMessage_ScenarioUserMessage_SetsAutoMessageDisplay()
   {
       var domainMsg = new ScenarioUserMessage("Hello", "Teaching step 1");
       var uiMsg = UIMessage.FromDomainMessage(domainMsg);

       Assert.AreEqual("Hello", uiMsg.Content);
       Assert.IsTrue(uiMsg.IsAutoMessage);
       Assert.AreEqual("Teaching step 1", uiMsg.Annotation);
       Assert.AreEqual("message-scenario-user", uiMsg.CssClass);
   }
   ```
   - Add case for ScenarioUserMessage (no logic)
   - Run test → Should FAIL

4. **GREEN: Implement ScenarioUserMessage conversion**
   - Set IsAutoMessage = true
   - Set Annotation
   - Set CSS class
   - Run test → Should PASS

5. **RED: Write tests for all other message types**
   - LlmTextMessage, LlmToolCallMessage, ToolResultMessage, etc.
   - Run tests → Should FAIL

6. **GREEN: Implement all UI conversions**
   - Add cases for all message types
   - Run tests → Should PASS

7. **REFACTOR: Clean up switch statement**

**Files Changed**:
- `TransparentAiAgentGui/Models/UIMessage.cs` (UPDATED)
- `TransparentAiAgentGui_Tests/Models/UIMessageConversionTests.cs` (CREATED)
- `TransparentAiAgentGui_Tests/Models/UIMessageTests.cs` (UPDATED)

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~UIMessageConversionTests"
# Expected: All tests pass
```

**Exit Criteria**:
- ✅ All message types have UI conversion
- ✅ Annotation display works
- ✅ CSS classes set correctly
- ✅ All tests pass

**Rollback**: Revert UIMessage.cs changes

---

## Phase 6: Migration & Cleanup

### Session 6.1: Migrate Existing Code to New Message Types

**Objective**: Update all existing code to use new message hierarchy

**Prerequisites**:
- All previous phases complete
- All new functionality tested and working

**Tasks**:

1. **Find all usages of old message types**
   ```bash
   grep -r "new UserMessage" TransparentAiAgentCore/
   grep -r "new AssistantMessage" TransparentAiAgentCore/
   grep -r "new AssistantToolCallMessage" TransparentAiAgentCore/
   ```

2. **Update GUI layer**
   - Update ConversationUIService
   - Update event handlers
   - Remove _pendingAutoMessages tracking (no longer needed)

3. **Update ScenarioExecutor**
   - Change to create ScenarioUserMessage instead of UserMessage
   - Pass annotation parameter

4. **Update AgentOrchestrator internal usages**
   - Use LlmTextMessage instead of AssistantMessage
   - Use LlmToolCallMessage instead of AssistantToolCallMessage
   - Use ToolResultMessage (new version)

5. **Run all tests**
   ```bash
   dotnet test
   # Expected: All tests pass
   ```

**Files Changed**:
- Multiple files across all layers

**Verification**:
```bash
dotnet build
# Expected: Clean build, no warnings
dotnet test
# Expected: All tests pass
grep -r "new UserMessage\|new AssistantMessage" TransparentAiAgentCore/
# Expected: No matches (all migrated)
```

**Exit Criteria**:
- ✅ All code uses new message types
- ✅ No references to old concrete UserMessage
- ✅ No references to old AssistantMessage
- ✅ All tests pass
- ✅ Clean build

**Rollback**: Revert all file changes in this session

---

### Session 6.2: Remove Old Message Types

**Objective**: Delete deprecated message types

**Prerequisites**:
- Session 6.1 complete (all code migrated)

**Tasks**:

1. **Verify no usages**
   ```bash
   grep -r "UserMessageOld\|AssistantMessageOld\|ToolResultMessageOld" .
   # Expected: No matches
   ```

2. **Delete old files**
   - Delete temporary "Old" versions if any
   - Delete old AssistantMessage.cs
   - Delete old AssistantToolCallMessage.cs if replaced

3. **Run all tests**
   ```bash
   dotnet test
   # Expected: All tests pass
   ```

4. **Clean build**
   ```bash
   dotnet clean
   dotnet build
   # Expected: Clean build
   ```

**Files Deleted**:
- Any temporary "Old" versions
- Old message type files if fully replaced

**Verification**:
```bash
dotnet build
# Expected: Clean build
dotnet test
# Expected: All tests pass
```

**Exit Criteria**:
- ✅ Old message types deleted
- ✅ No compilation errors
- ✅ All tests pass

**Rollback**: Restore deleted files from backup/version control

---

### Session 6.3: Final Verification and Documentation

**Objective**: Final end-to-end verification

**Prerequisites**:
- All previous sessions complete

**Tasks**:

1. **Run full test suite**
   ```bash
   dotnet test TransparentAiAgentCore_Tests
   dotnet test TransparentAiAgentGui_Tests
   # Expected: 100% pass
   ```

2. **Manual testing**
   - Start application
   - Send user message (DirectUserMessage)
   - Verify LLM responds
   - Verify messages display correctly in UI

3. **Test serialization**
   - Create conversation with various message types
   - Serialize to JSON
   - Deserialize from JSON
   - Verify all types restored correctly

4. **Documentation updates**
   - Update README if needed
   - Update architecture docs
   - Mark MESSAGE_ARCHITECTURE_REDESIGN.md as IMPLEMENTED

5. **Code review checklist**
   - [ ] All tests pass
   - [ ] No compilation warnings
   - [ ] XML documentation on public APIs
   - [ ] No TODOs left in code
   - [ ] Discriminators all unique and documented

**Verification**:
```bash
dotnet build -c Release
# Expected: Clean build, no warnings
dotnet test
# Expected: 100% pass
```

**Exit Criteria**:
- ✅ All tests pass
- ✅ Application runs correctly
- ✅ Serialization works
- ✅ Documentation updated
- ✅ Code review complete
- ✅ Ready to merge

---

## Summary

### Total Sessions: 17

**Phase 0**: 1 session (Baseline)
**Phase 1**: 5 sessions (Message Hierarchy)
**Phase 2**: 1 session (Pipeline)
**Phase 3**: 3 sessions (Orchestrator)
**Phase 4**: 1 session (Serialization)
**Phase 5**: 1 session (UI)
**Phase 6**: 3 sessions (Migration & Cleanup)
**Phase 7**: 2 sessions (Final Verification)

### Estimated Timeline

- **Baseline**: 0.5 hours
- **Phase 1**: 5-8 hours (1-1.5 hours per session)
- **Phase 2**: 1-2 hours
- **Phase 3**: 3-4 hours
- **Phase 4**: 1-2 hours
- **Phase 5**: 1-2 hours
- **Phase 6**: 3-5 hours
- **Total**: ~15-25 hours of implementation time

### Success Criteria

✅ All existing tests pass throughout
✅ New tests follow Lean TDD principles
✅ Code compiles at end of each session
✅ Each session is independently completable
✅ Rollback strategy exists for each session
✅ 4-tier message hierarchy fully implemented
✅ Discriminator-based serialization works
✅ UI correctly displays all message types
✅ No breaking changes to external APIs

### Risk Mitigation

1. **Parallel existence**: Old and new types coexist during migration
2. **Incremental testing**: Tests run after each session
3. **Clear rollback**: Each session has rollback steps
4. **Session independence**: Can pause between any sessions
5. **Verification gates**: Must pass tests before proceeding

---

**Note**: This plan follows Lean TDD principles - testing meaningful behavior that can fail due to bugs, not features enforced by the compiler. Each test verifies real logic, validation, or transformations.
