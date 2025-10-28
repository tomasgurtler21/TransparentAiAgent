# TDD Quick Reference

## MSTest Attributes

```csharp
[TestClass]          // Marks test class
public class MyTests
{
    [TestMethod]     // Marks individual test
    public void Test1() { }

    [TestInitialize] // Runs before each test (rarely needed)
    public void Setup() { }

    [TestCleanup]    // Runs after each test (rarely needed)
    public void Cleanup() { }

    [ClassInitialize] // Runs once before all tests in class
    public static void ClassSetup(TestContext context) { }

    [ClassCleanup]   // Runs once after all tests in class
    public static void ClassCleanup() { }
}
```

## MSTest Assertions

### Equality

```csharp
Assert.AreEqual(expected, actual);
Assert.AreEqual(expected, actual, "Custom message");
Assert.AreNotEqual(notExpected, actual);
```

### Identity (Same Object Reference)

```csharp
Assert.AreSame(expected, actual);
Assert.AreNotSame(notExpected, actual);
```

### Null Checks

```csharp
Assert.IsNull(obj);
Assert.IsNotNull(obj);
```

### Boolean

```csharp
Assert.IsTrue(condition);
Assert.IsFalse(condition);
```

### Type Checks

```csharp
Assert.IsInstanceOfType(obj, typeof(ExpectedType));
Assert.IsNotInstanceOfType(obj, typeof(UnexpectedType));
```

### Exceptions

```csharp
// Test that exception is thrown
Assert.ThrowsException<ArgumentException>(() => new MyClass(null));

// Test with custom message
Assert.ThrowsException<ConfigurationException>(
    () => config.Validate(),
    "Should throw when config is invalid"
);

// Capture exception to test properties
var ex = Assert.ThrowsException<MyException>(() => DoSomething());
Assert.AreEqual("Expected message", ex.Message);
```

### Collections

```csharp
CollectionAssert.Contains(collection, item);
CollectionAssert.DoesNotContain(collection, item);
CollectionAssert.AreEqual(expected, actual);
CollectionAssert.AreEquivalent(expected, actual); // Same items, any order
CollectionAssert.AllItemsAreNotNull(collection);
CollectionAssert.AllItemsAreUnique(collection);
```

### String Assertions

```csharp
StringAssert.Contains(text, substring);
StringAssert.StartsWith(text, prefix);
StringAssert.EndsWith(text, suffix);
StringAssert.Matches(text, pattern); // Regex pattern
```

## Test Naming Patterns

### Pattern: `{MethodOrScenario}_{StateUnderTest}_{ExpectedBehavior}`

```csharp
// Constructor tests
public void Constructor_NullParameter_ThrowsArgumentException()
public void Constructor_ValidInput_InitializesCorrectly()

// Method tests
public void Validate_NullProperty_ThrowsConfigurationException()
public void Validate_ValidConfiguration_DoesNotThrow()

// Service tests
public void LogEvent_ValidEvent_AddsToStore()
public void LogEvent_NullEvent_ThrowsArgumentNullException()

// Property tests (when they have logic)
public void ContextStatus_SetToTruncated_UpdatesSuccessfully()

// Conditional behavior
public void FormatContent_Success_ContainsSuccessMessage()
public void FormatContent_Error_ContainsErrorMessage()
```

## Test Commands

### Run All Tests

```bash
dotnet test TransparentAiAgentCore_Tests
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~UserMessageTests"
```

### Run Specific Test Method

```bash
dotnet test --filter "FullyQualifiedName~UserMessage_NullContent_ThrowsArgumentException"
```

### Run Tests by Category/Namespace

```bash
# All domain model tests
dotnet test --filter "FullyQualifiedName~Domain.Models"

# All exception tests
dotnet test --filter "FullyQualifiedName~Domain.Exceptions"

# All infrastructure tests
dotnet test --filter "FullyQualifiedName~Infrastructure"
```

### Run with Detailed Output

```bash
dotnet test --verbosity detailed
```

### Run with Code Coverage

```bash
dotnet test /p:CollectCoverage=true
```

## Decision Checklist

Before writing a test, ask these questions:

```
□ Does this test meaningful behavior?
  (validation, transformation, business rule)

□ Could this fail due to a bug?
  (not a compiler error)

□ Is there actual logic to test?
  (not trivial assignment)

□ Am I testing MY code?
  (not framework/compiler)
```

**If ALL checked**: Write the test ✅
**If ANY unchecked**: Skip the test ❌

## Test Organization

### File Structure

```
TransparentAiAgentCore_Tests/
├── Domain/
│   ├── Models/
│   │   ├── UserMessageTests.cs
│   │   ├── AssistantMessageTests.cs
│   │   └── ToolCallMessageTests.cs
│   ├── Exceptions/
│   │   └── ExceptionHierarchyTests.cs
│   ├── Configuration/
│   │   └── AgentConfigurationTests.cs
│   └── Transparency/
│       └── TransparencyEventTests.cs
└── Infrastructure/
    ├── Configuration/
    │   └── ConfigurationServiceTests.cs
    ├── Serialization/
    │   └── SerializationServiceTests.cs
    └── Transparency/
        └── TransparencyServiceTests.cs
```

### Test Class Template

```csharp
using TransparentAiAgentCore.Domain.Models;
using TransparentAiAgentCore.Domain.Exceptions;

namespace TransparentAiAgentCore_Tests.Domain.Models;

[TestClass]
public class MyClassTests
{
    // Optional: Setup before each test
    [TestInitialize]
    public void Setup()
    {
        // Initialize common test data
    }

    [TestMethod]
    public void Method_Scenario_ExpectedBehavior()
    {
        // Arrange
        var input = "test";

        // Act
        var result = DoSomething(input);

        // Assert
        Assert.AreEqual("expected", result);
    }
}
```

## Common Patterns Quick Reference

### Validation Pattern

```csharp
[TestMethod]
public void Method_NullInput_ThrowsArgumentException()
{
    Assert.ThrowsException<ArgumentException>(() => new MyClass(null));
}
```

### Default Value Pattern

```csharp
[TestMethod]
public void Constructor_ValidInput_SetsDefaultValue()
{
    var obj = new MyClass("input");
    Assert.AreEqual(ExpectedDefault, obj.Property);
}
```

### Uniqueness Pattern

```csharp
[TestMethod]
public void Constructor_MultipleInstances_GenerateUniqueIds()
{
    var obj1 = new MyClass();
    var obj2 = new MyClass();
    Assert.AreNotEqual(obj1.Id, obj2.Id);
}
```

### Service Behavior Pattern

```csharp
[TestMethod]
public void Service_Action_ProducesExpectedSideEffect()
{
    // Arrange
    var service = new MyService();

    // Act
    service.DoAction();

    // Assert
    Assert.AreEqual(1, service.GetActionCount());
}
```

### Event Handling Pattern

```csharp
[TestMethod]
public void Service_Action_RaisesEvent()
{
    // Arrange
    var service = new MyService();
    bool eventRaised = false;
    service.SomeEvent += (sender, e) => eventRaised = true;

    // Act
    service.DoAction();

    // Assert
    Assert.IsTrue(eventRaised);
}
```

### Thread Safety Pattern

```csharp
[TestMethod]
public void Service_ConcurrentAccess_ThreadSafe()
{
    var service = new MyService();
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => Task.Run(() => service.DoAction()))
        .ToArray();

    Task.WaitAll(tasks);

    Assert.AreEqual(10, service.GetActionCount());
}
```

## Success Metrics

A well-tested component has:

- ✅ **High value tests** - Every test verifies meaningful behavior
- ✅ **No redundant tests** - No tests for compiler/framework features
- ✅ **Clear test names** - Purpose obvious from name
- ✅ **Fast execution** - Tests run in milliseconds
- ✅ **Independent tests** - Tests don't depend on each other
- ✅ **Maintainable** - Easy to update when requirements change

## When to Update This Skill

Update when:

1. **New test patterns emerge** - Add to pattern library
2. **New anti-patterns discovered** - Document pitfalls
3. **Project conventions change** - Update guidelines
4. **Framework changes** - Update technical details
5. **Lean TDD principles evolve** - Refine decision framework
