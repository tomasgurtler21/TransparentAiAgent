---
name: tdd
description: Implements Test-Driven Development for .NET projects using Lean TDD principles - testing meaningful behavior, not compiler features. Guides through Red-Green-Refactor cycle with MSTest, focusing on validation logic, business rules, transformations, and service behavior while avoiding trivial tests.
---

# Lean Test-Driven Development

## Purpose

This skill guides implementation of Test-Driven Development (TDD) for the TransparentAiAgent project using **Lean TDD** principles - testing meaningful behavior that can fail due to bugs, NOT features enforced by the compiler or language.

## When to Use This Skill

Use this skill when:
- ✅ Implementing new classes with business logic
- ✅ Adding features that require validation or transformations
- ✅ Building services with complex behavior
- ✅ Need to ensure code quality through systematic testing

Skip this skill for:
- ❌ Simple enums without logic
- ❌ Plain interfaces without implementation
- ❌ Pure data transfer objects (DTOs) with no validation

## Project Context

- **Framework**: .NET 8.0, C#
- **Test Framework**: MSTest
- **Test Project**: `TransparentAiAgentCore_Tests`
- **Core Project**: `TransparentAiAgentCore`
- **Architecture**: Clean Architecture (Domain, Infrastructure, Application, Presentation)

---

## Core Principle: Lean TDD

**Test meaningful behavior that can fail due to bugs, NOT features enforced by the compiler or language.**

### What to Test ✅

1. **Validation Logic** - Can fail if validation is removed or has bugs
2. **Constructor Initialization** - Tests that initialization logic is executed correctly
3. **Transformations & Calculations** - Logic can have bugs
4. **Conditional Logic** - Branches can be wrong
5. **Service Behavior** - Complex logic with multiple interactions
6. **State Changes** - Verifies mutable state can be modified
7. **Exception Inheritance** - Tests inheritance chain is correct

### What NOT to Test ❌

1. **Enum Values** - If enum doesn't compile, you'll know
2. **Simple Property Access** - No logic to test
3. **Trivial Parameter Assignment** - No transformation, just storage
4. **Interface Definitions** - No implementation to test
5. **Framework Features** - Testing .NET, not your code

---

## TDD Workflow: Red → Green → Refactor

### Step 1: RED (Write Failing Test)

1. **Write test FIRST** before any implementation
2. **Run test** - it MUST fail (compilation error or assertion failure)
3. **Verify failure reason** - ensure it's failing for the right reason

```bash
dotnet test --filter "FullyQualifiedName~YourTestClass"
```

### Step 2: GREEN (Make Test Pass)

1. **Write MINIMUM code** to make test pass
2. **No extra features** - only what the test requires
3. **Run test** - it MUST pass

### Step 3: REFACTOR (Improve Code)

1. **Improve code quality** while keeping tests green
2. **Run tests** after each refactoring
3. **Don't add features** - only improve existing code

---

## Test Structure & Naming

### Test File Organization

Mirror the production code structure:

```
TransparentAiAgentCore_Tests/
├── Domain/
│   ├── Models/
│   │   └── UserMessageTests.cs
│   ├── Exceptions/
│   │   └── ExceptionHierarchyTests.cs
│   └── Configuration/
│       └── AgentConfigurationTests.cs
└── Infrastructure/
    ├── Serialization/
    │   └── SerializationServiceTests.cs
    └── Transparency/
        └── TransparencyServiceTests.cs
```

### Test Class Naming

**Pattern**: `{ClassUnderTest}Tests`

```csharp
[TestClass]
public class UserMessageTests { }

[TestClass]
public class TransparencyServiceTests { }
```

### Test Method Naming

**Pattern**: `{MethodOrScenario}_{StateUnderTest}_{ExpectedBehavior}`

```csharp
[TestMethod]
public void UserMessage_NullContent_ThrowsArgumentException()

[TestMethod]
public void UserMessage_ValidContent_DefaultsToInContext()

[TestMethod]
public void TransparencyService_LogEvent_AddsToStore()
```

### Test Method Structure

Use **Arrange-Act-Assert** pattern (AAA):

```csharp
[TestMethod]
public void ServiceMethod_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var service = new MyService();
    var input = "test data";

    // Act - Execute the method under test
    var result = service.DoSomething(input);

    // Assert - Verify expected behavior
    Assert.AreEqual("expected", result);
}
```

For simple tests, combine Arrange & Act:

```csharp
[TestMethod]
public void UserMessage_NullContent_ThrowsArgumentException()
{
    Assert.ThrowsException<ArgumentException>(() => new UserMessage(null));
}
```

---

## Decision Framework: To Test or Not to Test?

Before writing a test, ask:

- [ ] Does this test meaningful behavior (validation, transformation, business rule)?
- [ ] Could this fail due to a bug (not compiler error)?
- [ ] Is there actual logic to test (not trivial assignment)?
- [ ] Is this testing my code (not framework/compiler)?

**If ALL YES**: Write the test
**If ANY NO**: Skip the test

---

## Running Tests

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

### Run Tests by Namespace
```bash
# All domain model tests
dotnet test --filter "FullyQualifiedName~Domain.Models"

# All exception tests
dotnet test --filter "FullyQualifiedName~Domain.Exceptions"
```

---

## Common Pitfalls & Solutions

### Pitfall 1: Testing Trivial Properties

❌ **Bad**: Testing property getters with no logic
✅ **Good**: Skip these tests, focus on meaningful behavior

### Pitfall 2: Testing Multiple Things in One Test

❌ **Bad**: One giant test that verifies everything
✅ **Good**: Separate tests for each meaningful behavior

### Pitfall 3: Not Running Tests Before Implementation

❌ **Bad**: Write implementation first, then tests (not TDD)
✅ **Good**: Always write tests first, see them fail (RED), then implement (GREEN)

### Pitfall 4: Over-Complicated Test Setup

❌ **Bad**: Complex factories, builders, and setup code
✅ **Good**: Direct construction and minimal setup

---

## Success Metrics

A well-tested component has:

✅ **High value tests**: Every test verifies meaningful behavior
✅ **No redundant tests**: No tests for compiler/framework features
✅ **Clear test names**: Purpose obvious from name
✅ **Fast execution**: Tests run in milliseconds
✅ **Independent tests**: Tests don't depend on each other
✅ **Maintainable**: Easy to update when requirements change

---

**Remember**: The goal is **confidence in correctness**, not **test count**. One good test is better than five trivial ones.

For detailed examples and reference materials, see:
- `examples.md` - Comprehensive code examples (good vs bad)
- `reference.md` - Quick reference guide for MSTest patterns
