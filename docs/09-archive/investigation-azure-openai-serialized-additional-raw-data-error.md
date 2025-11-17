# Investigation: Azure OpenAI SerializedAdditionalRawData Error

**Date**: 2025-11-17
**Investigator**: Claude
**Status**: Root Cause Identified

---

## Error Description

When attempting to send LLM requests to Azure OpenAI, the application crashes with the following error:

```
Scenario execution failed: Unexpected error during streaming LLM request:
Method not found: 'System.Collections.Generic.IDictionary`2<System.String,System.BinaryData>
OpenAI.Chat.ChatCompletionOptions.get_SerializedAdditionalRawData()'.
```

**Location**: `TransparentAiAgentCore.Infrastructure.LLM.AzureOpenAIProvider`
**Occurs During**: Streaming LLM request execution

---

## Current Package Versions

From `TransparentAiAgentCore.csproj`:

```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.5.0-beta.1" />
<PackageReference Include="OpenAI" Version="2.7.0" />
```

---

## Root Cause Analysis

### What is SerializedAdditionalRawData?

`SerializedAdditionalRawData` is an **internal property** in the `OpenAI` SDK that serves as a dictionary to store additional properties received from API responses. This property is:

1. **Internal to the OpenAI package** - Not publicly accessible
2. **Only exposed to Azure.AI.OpenAI** through .NET's `InternalsVisibleTo` attribute
3. **Used for forward compatibility** - Preserves unknown fields from API responses

Reference: [OpenAI .NET Issue #259](https://github.com/openai/openai-dotnet/issues/259)

### The Version Compatibility Problem

The error occurs due to a **version mismatch** between the Azure.AI.OpenAI and OpenAI packages:

1. **Azure.AI.OpenAI 2.5.0-beta.1** (released 2025-10-03):
   - Requires: `OpenAI >= 2.5.0`
   - Compiled against OpenAI 2.5.0's internal API contract
   - Expects `SerializedAdditionalRawData` property with a specific signature

2. **OpenAI 2.7.0** (released 2025-11-13):
   - Is a later version than what Azure.AI.OpenAI 2.5.0-beta.1 was compiled against
   - The internal API contract (specifically `SerializedAdditionalRawData`) may have changed between 2.5.0 and 2.7.0
   - Changes to internal APIs are not documented in the public changelog

### Why This Happens

When Azure.AI.OpenAI 2.5.0-beta.1 tries to access the `SerializedAdditionalRawData` property on `ChatCompletionOptions`:

1. The compiler expects the property signature from OpenAI 2.5.0
2. At runtime, .NET loads OpenAI 2.7.0
3. The property signature doesn't match (or doesn't exist in the expected form)
4. **Result**: `MethodNotFoundException` at runtime

This is a classic example of the **DLL Hell problem** where internal dependencies between packages become incompatible across versions.

---

## Evidence From Code

The Azure OpenAI provider implementation in `AzureOpenAIProvider.cs`:

- **Line 167**: `var options = BuildChatCompletionOptions(request);` - Creates ChatCompletionOptions
- **Line 173**: `ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);`

The error occurs when the Azure.AI.OpenAI SDK internally tries to access `SerializedAdditionalRawData` on the `ChatCompletionOptions` object.

---

## Solutions

### Option 1: Upgrade Azure.AI.OpenAI (Recommended)

Upgrade to a version of Azure.AI.OpenAI that was compiled against OpenAI 2.7.0:

```xml
<!-- Check NuGet for the latest version compatible with OpenAI 2.7.0 -->
<PackageReference Include="Azure.AI.OpenAI" Version="2.7.0-beta.X" />
<PackageReference Include="OpenAI" Version="2.7.0" />
```

**Pros**:
- Uses latest features and bug fixes
- Forward compatibility

**Cons**:
- May introduce new breaking changes
- Requires testing

### Option 2: Downgrade OpenAI Package

Downgrade OpenAI to the version Azure.AI.OpenAI 2.5.0-beta.1 was built against:

```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.5.0-beta.1" />
<PackageReference Include="OpenAI" Version="2.5.0" />
```

**Pros**:
- Guaranteed compatibility
- Minimal code changes

**Cons**:
- Misses newer features in OpenAI 2.6.0-2.7.0
- Stays on beta versions

### Option 3: Use Stable Versions (Best for Production)

Use the latest **stable** (non-beta) versions of both packages:

```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
<!-- OpenAI version will be pulled as dependency -->
```

**Pros**:
- Production-ready, stable API
- Full support and documentation
- Tested compatibility

**Cons**:
- May lack bleeding-edge features available in beta

---

## Investigation Steps Taken

1. ✅ Rebased branch to `integration`
2. ✅ Reviewed project documentation in `docs/README.md`
3. ✅ Analyzed `AzureOpenAIProvider.cs` implementation
4. ✅ Checked package versions in `TransparentAiAgentCore.csproj`
5. ✅ Researched `SerializedAdditionalRawData` property and its purpose
6. ✅ Verified Azure.AI.OpenAI 2.5.0-beta.1 dependencies (requires OpenAI >= 2.5.0)
7. ✅ Reviewed OpenAI changelog for breaking changes between 2.5.0 and 2.7.0
8. ✅ Identified root cause as internal API version mismatch

---

## Recommendation

**For immediate resolution**: Use **Option 2** (downgrade OpenAI to 2.5.0) to match what Azure.AI.OpenAI 2.5.0-beta.1 expects.

**For long-term stability**: Use **Option 3** (stable versions) once tested.

---

## Testing Plan

After implementing the fix:

1. Clear all NuGet caches: `dotnet nuget locals all --clear`
2. Rebuild solution: `dotnet build --no-incremental`
3. Test Azure OpenAI streaming requests
4. Verify no `MethodNotFoundException` errors
5. Validate LLM responses are received correctly

---

## Related Files

- `TransparentAiAgentCore/TransparentAiAgentCore.csproj` - Package references
- `TransparentAiAgentCore/Infrastructure/LLM/AzureOpenAIProvider.cs` - Provider implementation
- `TransparentAiAgentCore/Domain/Configuration/AzureOpenAIConfiguration.cs` - Configuration model

---

## References

- [Azure.AI.OpenAI 2.5.0-beta.1 on NuGet](https://www.nuget.org/packages/Azure.AI.OpenAI/2.5.0-beta.1)
- [OpenAI 2.7.0 on NuGet](https://www.nuget.org/packages/OpenAI/2.7.0)
- [OpenAI .NET GitHub Issue #259 - SerializedAdditionalRawData](https://github.com/openai/openai-dotnet/issues/259)
- [Azure SDK Changelog](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/CHANGELOG.md)

---

## Next Steps

1. Decide which solution option to implement
2. Update package versions in `.csproj` file
3. Clear NuGet caches and rebuild
4. Test with Azure OpenAI endpoint
5. Update this document with test results

---

*This investigation was conducted on 2025-11-17 as part of troubleshooting Azure OpenAI provider issues.*
