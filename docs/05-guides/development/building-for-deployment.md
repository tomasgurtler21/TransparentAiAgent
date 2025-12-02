# Building for Deployment

This guide explains how to build the TransparentAiAgent application for deployment on Windows systems.

## Overview

The TransparentAiAgent is configured for **self-contained deployment** on **Windows (x64)**, which means:
- The .NET 8 runtime is included in the deployment package
- Users don't need to install .NET separately
- The package contains all dependencies required to run the application

## Prerequisites

- **Visual Studio 2022** (recommended) or **.NET 8 SDK**

## Build Method: Visual Studio (Recommended)

The simplest way to build a deployment package:

1. Open `TransparentAiAgentGui.sln` in Visual Studio
2. Right-click the **TransparentAiAgentGui** project
3. Select **Publish**
4. Choose **Folder** as the target
5. Set configuration to **Release**
6. Click **Publish**

The output will be in `bin/Release/net8.0/win-x64/publish/`

## Alternative: Command Line

If you prefer using the command line:

```powershell
# Navigate to the solution root
cd C:\programming\TransparentAiAgent\integration\TransparentAiAgent

# Publish for Windows x64 (self-contained)
dotnet publish TransparentAiAgentGui/TransparentAiAgentGui.csproj -c Release
```

The project is already configured for self-contained Windows x64 deployment in Release mode.

## Build Output

After building, the publish directory will contain:

```
publish/win-x64/
├── TransparentAiAgentGui.exe          # Main executable
├── appsettings.json                    # Configuration file (users will edit this)
├── wwwroot/                            # Web assets (CSS, JS)
│   ├── app.css
│   ├── favicon.png
│   └── ...
├── data/                               # Application data folders
│   ├── knowledge/                      # Knowledge library entries
│   ├── scenarios/                      # Teaching mode scenarios
│   ├── conversations/                  # Created at runtime
│   └── memory/                         # Created at runtime
├── Anthropic.Client.dll                # LLM provider libraries
├── Azure.AI.OpenAI.dll
├── OpenAI.dll
└── [Other DLL dependencies]
```

## Configuration Before Deployment

Before distributing, edit `appsettings.json`:

- **Remove any API keys** (users should add their own)
- Set reasonable default values for all settings
- Make sure `ActiveProvider` is set to a sensible default

**Important:** Don't include your personal API keys in the distributed package!

## Verify Data Folders

Check that the following folders exist in the publish output:
- `data/knowledge/` - Should contain default knowledge entries
- `data/scenarios/` - Should contain teaching mode scenarios
- `data/conversations/` - Will be created at runtime if missing
- `data/memory/` - Will be created at runtime if missing

## Creating a Distribution Package

To create a ZIP file for easy distribution:

```powershell
# Navigate to your publish output folder
cd bin\Release\net8.0\win-x64\publish

# Create ZIP (PowerShell)
Compress-Archive -Path * -DestinationPath ..\TransparentAiAgent-win-x64.zip
```

Or simply select all files in the publish folder, right-click, and choose "Send to > Compressed (zipped) folder" in Windows Explorer.

## Build Troubleshooting

### Issue: "Could not find a part of the path '...lib\Anthropic.Client'"

**Cause:** The Anthropic.Client DLL is missing from the `lib/` folder.

**Solution:**
```powershell
# Rebuild the Anthropic SDK and copy the DLL
cd path\to\anthropic-sdk-csharp\src\Anthropic.Client
dotnet build
copy bin\Debug\net8.0\Anthropic.Client.* path\to\TransparentAiAgent\lib\Anthropic.Client\
```

### Issue: "System.Text.Json version conflict"

**Cause:** The Anthropic.Client was built against a newer version of System.Text.Json.

**Solution:** The `TransparentAiAgentCore.csproj` already includes `System.Text.Json` v9.0.1. If the issue persists, update to the latest version:
```xml
<PackageReference Include="System.Text.Json" Version="9.0.1" />
```

### Issue: Build succeeds but executable doesn't run

**Cause:** Missing runtime dependencies or configuration files.

**Solution:**
1. Verify all DLLs are in the publish folder
2. Check that `appsettings.json` exists and is valid JSON
3. Ensure the `data/` folder structure is correct
4. Test on a clean machine without .NET installed (self-contained should work)

## Versioning

To update the application version:

1. Edit `TransparentAiAgentGui/TransparentAiAgentGui.csproj`
2. Add or update the `<Version>` property:
   ```xml
   <PropertyGroup>
     <Version>1.0.0</Version>
   </PropertyGroup>
   ```
3. Rebuild the application

The version will be embedded in the executable and visible in file properties.

## Next Steps

After building the deployment package:

1. **Test the build** - Run on a clean Windows machine without .NET
2. **Review configuration** - Ensure no sensitive data is included
3. **Package for distribution** - Create ZIP or installer
4. **Write release notes** - Document what's new and any breaking changes

See also:
- [Installation Guide](installation-guide.md) - How to install and configure
- [Folder Structure](folder-structure.md) - Understanding the deployment layout
- [Configuration Guide](configuration-guide.md) - Configuring LLM providers
