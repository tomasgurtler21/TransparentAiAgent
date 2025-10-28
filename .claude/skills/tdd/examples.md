# TDD Examples - Good vs Bad

This document contains concrete examples of what to test and what not to test, following Lean TDD principles.

## Examples of What TO TEST ✅

### 1. Validation Logic

**Why**: Can fail if validation is removed or has bugs

```csharp
// ✅ GOOD - Tests validation behavior
[TestMethod]
public void UserMessage_NullContent_ThrowsArgumentException()
{
    Assert.ThrowsException<ArgumentException>(() => new UserMessage(null));
}

[TestMethod]
public void AgentConfiguration_ContextWindowSizeZero_ThrowsConfigurationException()
{
    var config = new AgentConfiguration { ContextWindowSize = 0 };
    Assert.ThrowsException<ConfigurationException>(() => config.Validate());
}
```

### 2. Constructor Initialization with Business Meaning

**Why**: Tests that initialization logic is executed correctly

```csharp
// ✅ GOOD - Tests that ID is actually generated
[TestMethod]
public void UserMessage_GeneratesUniqueIds()
{
    var msg1 = new UserMessage("Hi");
    var msg2 = new UserMessage("Hello");
    Assert.AreNotEqual(msg1.Id, msg2.Id);
    Assert.AreNotEqual(Guid.Empty, msg1.Id);
}

// ✅ GOOD - Tests business rule (default value)
[TestMethod]
public void UserMessage_DefaultsToInContext()
{
    var msg = new UserMessage("Hi");
    Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
}
```

### 3. Transformations & Calculations

**Why**: Logic can have bugs

```csharp
// ✅ GOOD - Tests Content formatting logic
[TestMethod]
public void ToolCallMessage_FormatsContentCorrectly()
{
    var msg = new ToolCallMessage("get_weather", "{\"city\":\"Prague\"}", "call-123");
    Assert.IsTrue(msg.Content.Contains("Tool Call:"));
    Assert.IsTrue(msg.Content.Contains("get_weather"));
}

// ✅ GOOD - Tests null parameter defaults to empty JSON
[TestMethod]
public void ToolCallMessage_NullParameters_DefaultsToEmptyJson()
{
    var msg = new ToolCallMessage("tool", null, "call-123");
    Assert.AreEqual("{}", msg.ToolParameters);
}
```

### 4. Conditional Logic

**Why**: Branches can be wrong

```csharp
// ✅ GOOD - Tests conditional formatting
[TestMethod]
public void ToolResultMessage_Success_FormatsContentCorrectly()
{
    var msg = new ToolResultMessage("call-123", "tool", "{\"temp\":20}", true);
    Assert.IsTrue(msg.Content.Contains("Tool Result:"));
}

[TestMethod]
public void ToolResultMessage_Error_FormatsContentCorrectly()
{
    var msg = new ToolResultMessage("call-123", "tool", null, false, "Error");
    Assert.IsTrue(msg.Content.Contains("Tool Error:"));
}
```

### 5. Service Behavior

**Why**: Complex logic with multiple interactions

```csharp
// ✅ GOOD - Tests service behavior
[TestMethod]
public void TransparencyService_LogEvent_AddsToStore()
{
    var service = new TransparencyService();
    var evt = new TransparencyEvent(TransparencyEventType.UserInput, "data");

    service.LogEvent(evt);

    Assert.AreEqual(1, service.GetEvents().Count());
}

// ✅ GOOD - Tests event emission
[TestMethod]
public void TransparencyService_LogEvent_RaisesEventLoggedEvent()
{
    var service = new TransparencyService();
    var evt = new TransparencyEvent(TransparencyEventType.UserInput, "data");
    TransparencyEvent? raisedEvent = null;
    service.EventLogged += (sender, e) => raisedEvent = e;

    service.LogEvent(evt);

    Assert.IsNotNull(raisedEvent);
    Assert.AreSame(evt, raisedEvent);
}
```

### 6. State Changes

**Why**: Verifies mutable state can be modified

```csharp
// ✅ GOOD - Tests that setter works
[TestMethod]
public void UserMessage_ContextStatus_CanBeUpdated()
{
    var msg = new UserMessage("Hello");
    msg.ContextStatus = MessageContextStatus.TruncatedFromContext;
    Assert.AreEqual(MessageContextStatus.TruncatedFromContext, msg.ContextStatus);
}
```

### 7. Exception Inheritance & Behavior

**Why**: Tests inheritance chain is correct

```csharp
// ✅ GOOD - Tests inheritance
[TestMethod]
public void ConfigurationException_InheritsFromAgentException()
{
    try
    {
        throw new ConfigurationException("Config error");
    }
    catch (AgentException ex)
    {
        Assert.IsInstanceOfType(ex, typeof(ConfigurationException));
    }
}

// ✅ GOOD - Tests message preservation
[TestMethod]
public void AgentException_PreservesMessage()
{
    var exception = new AgentException("test message");
    Assert.AreEqual("test message", exception.Message);
}

// ✅ GOOD - Tests inner exception preservation
[TestMethod]
public void AgentException_PreservesInnerException()
{
    var inner = new InvalidOperationException("inner");
    var exception = new AgentException("outer", inner);

    Assert.AreEqual("outer", exception.Message);
    Assert.AreSame(inner, exception.InnerException);
}
```

---

## Examples of What NOT TO TEST ❌

### 1. Enum Values Exist

**Why**: If enum doesn't compile, you'll know

```csharp
// ❌ BAD - Waste of time
[TestMethod]
public void MessageRole_HasUserValue()
{
    var role = MessageRole.User;
    Assert.AreEqual(MessageRole.User, role); // Pointless
}
```

### 2. Simple Property Access

**Why**: No logic to test

```csharp
// ❌ BAD - Testing compiler
[TestMethod]
public void UserMessage_Role_IsUser()
{
    var msg = new UserMessage("Hi");
    Assert.AreEqual(MessageRole.User, msg.Role); // No logic here
}
```

### 3. Trivial Parameter Assignment

**Why**: No transformation, just storage

```csharp
// ❌ BAD - Trivial assignment
[TestMethod]
public void UserMessage_Content_IsStored()
{
    var msg = new UserMessage("Hello");
    Assert.AreEqual("Hello", msg.Content); // Just assigns parameter
}
```

### 4. Interface Definitions

**Why**: No implementation to test

```csharp
// ❌ BAD - Interface has no logic
[TestMethod]
public void IMessage_HasIdProperty()
{
    // Don't test interface definitions
}
```

### 5. Framework Features

**Why**: Testing .NET, not your code

```csharp
// ❌ BAD - Testing Guid.NewGuid()
[TestMethod]
public void GuidNewGuid_CreatesValidGuid()
{
    var guid = Guid.NewGuid();
    Assert.AreNotEqual(Guid.Empty, guid); // Testing .NET
}
```

---

## Complete TDD Cycle Example

### Example: Implementing UserMessage

#### Step 1: Write Tests First (RED)

```csharp
// File: UserMessageTests.cs
[TestClass]
public class UserMessageTests
{
    [TestMethod]
    public void UserMessage_NullContent_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => new UserMessage(null));
    }

    [TestMethod]
    public void UserMessage_DefaultsToInContext()
    {
        var msg = new UserMessage("Hello");
        Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
    }

    [TestMethod]
    public void UserMessage_GeneratesUniqueIds()
    {
        var msg1 = new UserMessage("Hi");
        var msg2 = new UserMessage("Hello");
        Assert.AreNotEqual(msg1.Id, msg2.Id);
    }

    [TestMethod]
    public void UserMessage_ContextStatus_CanBeUpdated()
    {
        var msg = new UserMessage("Hello");
        msg.ContextStatus = MessageContextStatus.TruncatedFromContext;
        Assert.AreEqual(MessageContextStatus.TruncatedFromContext, msg.ContextStatus);
    }
}
```

**Run tests** - They FAIL (class doesn't exist yet) ✅

#### Step 2: Implement Minimum Code (GREEN)

```csharp
// File: UserMessage.cs
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
        ContextStatus = MessageContextStatus.InContext;
    }
}
```

**Run tests** - They PASS ✅

#### Step 3: Refactor (if needed)

- Review code quality
- Improve naming, structure
- Keep tests green

---

## Common Test Patterns

### Pattern 1: Validation Tests

```csharp
[TestMethod]
public void Constructor_NullParameter_ThrowsArgumentException()
{
    Assert.ThrowsException<ArgumentException>(() => new MyClass(null));
}

[TestMethod]
public void Constructor_EmptyParameter_ThrowsArgumentException()
{
    Assert.ThrowsException<ArgumentException>(() => new MyClass(""));
}

[TestMethod]
public void Constructor_WhitespaceParameter_ThrowsArgumentException()
{
    Assert.ThrowsException<ArgumentException>(() => new MyClass("   "));
}
```

### Pattern 2: Default Value Tests

```csharp
[TestMethod]
public void Constructor_ValidInput_SetsDefaultStatus()
{
    var obj = new MyClass("valid");
    Assert.AreEqual(ExpectedStatus.Default, obj.Status);
}
```

### Pattern 3: Uniqueness Tests

```csharp
[TestMethod]
public void Constructor_MultipleInstances_GenerateUniqueIds()
{
    var obj1 = new MyClass("test1");
    var obj2 = new MyClass("test2");

    Assert.AreNotEqual(obj1.Id, obj2.Id);
    Assert.AreNotEqual(Guid.Empty, obj1.Id);
    Assert.AreNotEqual(Guid.Empty, obj2.Id);
}
```

### Pattern 4: Transformation Tests

```csharp
[TestMethod]
public void Method_TransformsInputCorrectly()
{
    var obj = new MyClass();
    var result = obj.Transform("input");

    Assert.IsTrue(result.Contains("expected"));
    Assert.AreEqual("expected format", result);
}
```

### Pattern 5: Service Behavior Tests

```csharp
[TestMethod]
public void Service_MethodCall_ProducesExpectedSideEffect()
{
    // Arrange
    var service = new MyService();
    var input = new MyInput();

    // Act
    service.DoSomething(input);

    // Assert
    var result = service.GetResult();
    Assert.IsNotNull(result);
    Assert.AreEqual(1, result.Count);
}
```

### Pattern 6: Thread Safety Tests

```csharp
[TestMethod]
public void Service_ThreadSafe_MultipleThreads()
{
    // Arrange
    const int threadCount = 10;
    const int operationsPerThread = 100;
    var service = new MyService();
    var tasks = new Task[threadCount];

    // Act
    for (int i = 0; i < threadCount; i++)
    {
        tasks[i] = Task.Run(() =>
        {
            for (int j = 0; j < operationsPerThread; j++)
            {
                service.DoSomething();
            }
        });
    }

    Task.WaitAll(tasks);

    // Assert
    Assert.AreEqual(threadCount * operationsPerThread, service.GetOperationCount());
}
```

---

## Pitfalls with Examples

### Pitfall 1: Testing Trivial Properties

❌ **Bad**:
```csharp
[TestMethod]
public void UserMessage_Role_ReturnsUser()
{
    var msg = new UserMessage("Hi");
    Assert.AreEqual(MessageRole.User, msg.Role); // No logic to test
}
```

✅ **Good**: Skip this test, it's trivial

---

### Pitfall 2: Testing Multiple Things in One Test

❌ **Bad**:
```csharp
[TestMethod]
public void UserMessage_ConstructorSetsEverything()
{
    var msg = new UserMessage("Hello");
    Assert.IsNotNull(msg.Id);
    Assert.AreEqual(MessageRole.User, msg.Role);
    Assert.AreEqual("Hello", msg.Content);
    Assert.IsNotNull(msg.Timestamp);
    Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
}
```

✅ **Good**: Separate tests for meaningful behaviors only

```csharp
[TestMethod]
public void UserMessage_GeneratesUniqueIds() { /* test ID generation */ }

[TestMethod]
public void UserMessage_DefaultsToInContext() { /* test default status */ }

// Skip testing trivial assignments (Role, Content, Timestamp)
```

---

### Pitfall 3: Not Running Tests Before Implementation

❌ **Bad**: Write implementation first, then tests (not TDD)

✅ **Good**: Always write tests first, see them fail (RED), then implement (GREEN)

---

### Pitfall 4: Over-Complicated Test Setup

❌ **Bad**:
```csharp
[TestMethod]
public void SimpleTest()
{
    var factory = new MessageFactory();
    var builder = new MessageBuilder();
    var validator = new MessageValidator();
    var msg = builder.WithContent("Hi").WithValidator(validator).Build();
    // Too much setup for simple test
}
```

✅ **Good**:
```csharp
[TestMethod]
public void SimpleTest()
{
    var msg = new UserMessage("Hi"); // Direct construction
    Assert.AreEqual(MessageContextStatus.InContext, msg.ContextStatus);
}
```
