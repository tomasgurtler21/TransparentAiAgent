using System.Globalization;
using TransparentAiAgentGui.Components;
using TransparentAiAgentGui.Services;
using TransparentAiAgentCore.Application.Agent;
using TransparentAiAgentCore.Application.Conversation;
using TransparentAiAgentCore.Application.Pipeline;
using TransparentAiAgentCore.Infrastructure.Configuration;
using TransparentAiAgentCore.Infrastructure.Transparency;
using TransparentAiAgentCore.Infrastructure.Serialization;
using TransparentAiAgentCore.Infrastructure.Authentication;
using TransparentAiAgentCore.Infrastructure.LLM;
using TransparentAiAgentCore.Domain.Configuration;
using TransparentAiAgentCore.Domain.Authentication;
using TransparentAiAgentCore.Domain.LLM;
using TransparentAiAgentCore.Domain.Tools;
using TransparentAiAgentCore.Application.Tools;
using TransparentAiAgentCore.Infrastructure.Tools;
using TransparentAiAgentCore.Infrastructure.Tools.MCP;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInUIControl;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInKnowledge;
using TransparentAiAgentCore.Infrastructure.Tools.Validation;
using TransparentAiAgentCore.Domain.UIControl;
using TransparentAiAgentCore.Domain.Scenarios;
using TransparentAiAgentCore.Application.Scenarios;
using TransparentAiAgentCore.Infrastructure.Scenarios;
using TransparentAiAgentCore.Domain.Knowledge;
using TransparentAiAgentCore.Infrastructure.Knowledge;
using TransparentAiAgentCore.Application.Teaching;
using TransparentAiAgentCore.Domain.ConversationHistory;
using TransparentAiAgentCore.Application.ConversationHistory;
using TransparentAiAgentCore.Infrastructure.ConversationHistory;

// Force InvariantCulture for the entire application to avoid locale-specific number parsing issues
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options for HTTP/API endpoints to use InvariantCulture
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Core Infrastructure services
builder.Services.AddSingleton<ITransparencyService, TransparencyService>();
builder.Services.AddSingleton<ISerializationService, SerializationService>();
builder.Services.AddSingleton<IToolUsageStatistics, ToolUsageStatistics>();
builder.Services.AddSingleton<ToolSchemaValidator>();

// Register Conversation History services
builder.Services.AddSingleton<MessageSerializer>();
builder.Services.AddSingleton<IConversationRepository>(sp =>
{
    var messageSerializer = sp.GetRequiredService<MessageSerializer>();
    var logger = sp.GetRequiredService<ILogger<JsonConversationRepository>>();
    var conversationsPath = Path.Combine(builder.Environment.ContentRootPath, "conversations");
    return new JsonConversationRepository(messageSerializer, logger, conversationsPath);
});
builder.Services.AddScoped<IConversationHistoryManager, ConversationHistoryManager>();

// Load configuration
var configService = new ConfigurationService();
var appConfig = new AppConfiguration(); // Use defaults for now

// Try to load from appsettings if available
try
{
    var configPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
    if (File.Exists(configPath))
    {
        var loadedConfig = configService.LoadConfiguration(configPath);
        appConfig = loadedConfig;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not load configuration from appsettings.json: {ex.Message}");
    Console.WriteLine("Using default configuration. The app will start but LLM features will not be available.");
}

// Register the SAME ConfigurationService instance that we just loaded (not a new one!)
builder.Services.AddSingleton<IConfigurationService>(configService);
builder.Services.AddSingleton(appConfig);

// Check if LLM configuration is valid
bool isLLMConfigured = false;
string? llmConfigurationError = null;
try
{
    appConfig.LLM.Validate();
    isLLMConfigured = true;
}
catch (Exception ex)
{
    llmConfigurationError = ex.Message;
    Console.WriteLine($"⚠ LLM configuration is invalid: {ex.Message}");
    Console.WriteLine("The app will start but LLM features will not be available.");
    Console.WriteLine("Please configure LLM settings in appsettings.json to use agent features.");
    Console.WriteLine("See docs/CONFIGURATION_SETUP.md for detailed instructions.");
}

// Register Authentication
builder.Services.AddSingleton<IAuthenticationProvider, ConfigurationAuthenticationProvider>();

// Register Message Pipeline (always needed)
builder.Services.AddSingleton<IMessagePipeline, MessagePipeline>();

// Register Conversation Manager (always needed) - Scoped for per-user isolation
builder.Services.AddScoped<IConversationManager>(sp =>
{
    var config = sp.GetRequiredService<AppConfiguration>();
    var transparencyService = sp.GetRequiredService<ITransparencyService>();
    var configurationOverlay = sp.GetRequiredService<IConfigurationOverlay>();
    return new ConversationManager(config.Agent.ContextWindowSize, transparencyService, configurationOverlay);
});

// Register Knowledge Library services (Phase 11 - Knowledge Library)
builder.Services.AddSingleton<IKnowledgeLibrary>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<JsonKnowledgeLibrary>>();
    var knowledgeBasePath = Path.Combine(builder.Environment.WebRootPath, "knowledge");
    return new JsonKnowledgeLibrary(knowledgeBasePath, logger);
});
builder.Services.AddSingleton<BuiltInKnowledgeToolRegistry>();
builder.Services.AddScoped<KnowledgeLibraryToolExecutor>();
builder.Services.AddSingleton<TeachingModePromptBuilder>();

// Register Tool services (if tools are enabled)
if (appConfig.Agent.EnableTools)
{
    // Register IToolRegistry first (needed by UI components like ToolsOverview)
    // IMPORTANT: Tool discovery will be triggered synchronously after app is built
    // to avoid race conditions where tools aren't available on first request
    builder.Services.AddSingleton<IToolRegistry>(sp =>
    {
        try
        {
            // Create MCP Tool Registry
            var mcpRegistry = new MCPToolRegistry(appConfig.MCP);

            // Create Built-in UI Control Tool Registry (Phase 9)
            var uiControlRegistry = sp.GetRequiredService<BuiltInUIControlToolRegistry>();

            // Create Built-in Knowledge Tool Registry (Phase 11)
            var knowledgeRegistry = sp.GetRequiredService<BuiltInKnowledgeToolRegistry>();

            // Create Tool Registry Composite (MCP + UI Control + Knowledge)
            var compositeRegistry = new ToolRegistryComposite(new IToolRegistry[] { mcpRegistry, uiControlRegistry, knowledgeRegistry });

            // NOTE: Tool discovery will be triggered synchronously AFTER app.Build()
            // to ensure tools are available before accepting requests

            return compositeRegistry;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Failed to initialize tool registry: {ex.Message}");
            throw;
        }
    });

    // Register IToolManager - always register when tools are enabled (UI control tools work without MCP)
    builder.Services.AddScoped<IToolManager>(sp =>
    {
        try
        {
            var transparencyService = sp.GetRequiredService<ITransparencyService>();
            var toolRegistry = sp.GetRequiredService<IToolRegistry>();

            // Create UI Control Tool Executor (Phase 9) - always available
            var uiControlExecutor = sp.GetRequiredService<UIControlToolExecutor>();

            // Create Knowledge Library Tool Executor (Phase 11) - always available
            var knowledgeExecutor = sp.GetRequiredService<KnowledgeLibraryToolExecutor>();

            // Get tool usage statistics service
            var statistics = sp.GetRequiredService<IToolUsageStatistics>();

            // Get tool schema validator service (Phase 9b - Tool Execution Safety)
            var validator = sp.GetRequiredService<ToolSchemaValidator>();

            // Build list of executors (UI Control + Knowledge always available)
            var executors = new List<IToolExecutor> { uiControlExecutor, knowledgeExecutor };

            // Add MCP executor only if MCP servers are configured
            if (appConfig.MCP.Servers.Count > 0)
            {
                // Create MCP Tool Discovery
                var mcpDiscovery = new MCPToolDiscovery(appConfig.MCP);

                // Create MCP Tool Executor
                var mcpExecutor = new MCPToolExecutor(mcpDiscovery);
                executors.Add(mcpExecutor);

                Console.WriteLine($"✓ Tool system enabled with {appConfig.MCP.Servers.Count} MCP server(s) + UI control tools + knowledge library tools");
            }
            else
            {
                Console.WriteLine("✓ Tool system enabled with UI control tools + knowledge library tools only (no MCP servers configured)");
            }

            // Create Tool Manager with all available executors
            var toolManager = new ToolManager(
                toolRegistry,
                executors,
                transparencyService,
                statistics,
                validator);

            return toolManager;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Failed to initialize tool manager: {ex.Message}");
            throw;
        }
    });
}

// Conditionally register LLM services based on configuration validity
if (isLLMConfigured)
{
    // Full LLM stack with real implementation
    builder.Services.AddSingleton<LLMProviderFactory>();
    builder.Services.AddSingleton<ILLMProvider>(sp =>
    {
        var factory = sp.GetRequiredService<LLMProviderFactory>();
        return factory.CreateProvider();
    });
    // Scoped to support scoped IToolManager and IConversationManager
    builder.Services.AddScoped<IAgentOrchestrator>(sp =>
    {
        var llmProvider = sp.GetRequiredService<ILLMProvider>();
        var conversationManager = sp.GetRequiredService<IConversationManager>();
        var messagePipeline = sp.GetRequiredService<IMessagePipeline>();
        var transparencyService = sp.GetRequiredService<ITransparencyService>();
        var config = sp.GetRequiredService<AppConfiguration>();
        var toolMgr = sp.GetService<IToolManager>(); // Optional

        return new AgentOrchestrator(
            llmProvider,
            conversationManager,
            messagePipeline,
            transparencyService,
            config,
            toolMgr);
    });

    Console.WriteLine($"✓ LLM Provider configured: {appConfig.LLM.Provider}");
}
else
{
    // Register stub implementation that throws helpful errors with details - Scoped to match main registration
    builder.Services.AddScoped<IAgentOrchestrator>(sp =>
    {
        var conversationManager = sp.GetRequiredService<IConversationManager>();
        return new NotConfiguredAgentOrchestrator(conversationManager, llmConfigurationError);
    });

    Console.WriteLine("⚠ LLM not configured. Agent will show configuration error when used.");
    Console.WriteLine("  Configure LLM settings in appsettings.json to enable agent features.");
    Console.WriteLine("  See docs/CONFIGURATION_SETUP.md for instructions.");
}

// Register UI services
builder.Services.AddScoped<IConversationUIService, ConversationUIService>();

// Register UI Control services (Phase 9 - Teaching Mode)
builder.Services.AddSingleton<IUIControlService, UIControlService>();  // Singleton to share across all render contexts
builder.Services.AddSingleton<BuiltInUIControlToolRegistry>();
builder.Services.AddScoped<UIControlToolExecutor>();  // Scoped to work with scoped IToolManager

// Register App Mode service (Phase 9d - Teaching Mode System)
builder.Services.AddScoped<IAppModeService, AppModeService>();  // Scoped to match ConversationManager lifetime

// Register Scenario services (Phase 10a/10b - Teaching Mode Scenarios)
builder.Services.AddSingleton<IScenarioRegistry, ScenarioRegistry>();
builder.Services.AddScoped<IConfigurationOverlay>(sp =>
{
    // Initialize with empty base configuration - could be expanded to load from appsettings if needed
    var baseConfig = new Dictionary<string, object>();
    return new ConfigurationOverlayService(baseConfig);
});
builder.Services.AddScoped<IConditionEvaluator, ConditionEvaluator>();
builder.Services.AddScoped<IScenarioExecutor, ScenarioExecutor>();

// Register HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

var app = builder.Build();

// Load scenarios from JSON files (Phase 10a - Teaching Mode Scenarios)
try
{
    var scenariosPath = Path.Combine(builder.Environment.WebRootPath, "scenarios");

    if (Directory.Exists(scenariosPath))
    {
        Console.WriteLine($"⏳ Loading teaching scenarios from {scenariosPath}...");

        var scenarioLoader = new JsonScenarioLoader();
        var scenarios = await scenarioLoader.LoadAllFromDirectoryAsync(scenariosPath);

        var scenarioRegistry = app.Services.GetRequiredService<IScenarioRegistry>();
        foreach (var scenario in scenarios)
        {
            scenarioRegistry.AddScenario(scenario);
        }

        Console.WriteLine($"✓ Loaded {scenarios.Count} teaching scenario(s)");
        if (scenarios.Count > 0)
        {
            Console.WriteLine("   Scenarios:");
            foreach (var scenario in scenarios.OrderBy(s => s.Name))
            {
                Console.WriteLine($"     - {scenario.Name} ({scenario.Steps.Count} steps)");
            }
        }
    }
    else
    {
        Console.WriteLine($"⚠ Scenarios directory not found: {scenariosPath}");
        Console.WriteLine("   No teaching scenarios will be available.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ Failed to load teaching scenarios: {ex.Message}");
    Console.WriteLine("   The app will start but teaching scenarios will not be available.");
}

// CRITICAL FIX: Discover tools synchronously BEFORE accepting requests
// This prevents race condition where tools aren't available on first request
if (appConfig.Agent.EnableTools && appConfig.MCP.AutoDiscoverTools && appConfig.MCP.Servers.Count > 0)
{
    try
    {
        Console.WriteLine($"⏳ Discovering tools from {appConfig.MCP.Servers.Count} MCP server(s)...");
        Console.WriteLine($"   MCP Servers: {string.Join(", ", appConfig.MCP.Servers.Select(s => s.Name))}");

        var toolRegistry = app.Services.GetRequiredService<IToolRegistry>();
        Console.WriteLine($"   Tool registry type: {toolRegistry.GetType().Name}");

        await toolRegistry.RefreshAsync();

        var tools = toolRegistry.GetAllTools();
        Console.WriteLine($"✓ Discovered {tools.Count} tools from {appConfig.MCP.Servers.Count} MCP server(s)");

        // Log each tool for verification
        if (tools.Count > 0)
        {
            Console.WriteLine("   Tools:");
            foreach (var tool in tools)
            {
                Console.WriteLine($"     - {tool.Name} ({tool.SourceType})");
            }
        }
        else
        {
            Console.WriteLine("   ⚠ WARNING: No tools were discovered!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Tool discovery failed: {ex.Message}");
        Console.WriteLine($"   Exception type: {ex.GetType().Name}");
        Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        Console.WriteLine("   The app will start but MCP tools will not be available.");
    }
}
else
{
    Console.WriteLine($"⚠ Tool discovery skipped:");
    Console.WriteLine($"   EnableTools: {appConfig.Agent.EnableTools}");
    Console.WriteLine($"   AutoDiscoverTools: {appConfig.MCP.AutoDiscoverTools}");
    Console.WriteLine($"   MCP Servers count: {appConfig.MCP.Servers.Count}");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Configuration API endpoints
app.MapGet("/api/config", (IConfigurationService configService) =>
{
    var config = configService.GetConfiguration();
    return Results.Ok(new
    {
        Agent = new
        {
            config.Agent.SystemPrompt,
            config.Agent.ContextWindowSize,
            config.Agent.EnableTools
        },
        LLM = new
        {
            config.LLM.Provider,
            config.LLM.Temperature,
            config.LLM.MaxTokens,
            config.LLM.TopP
        }
    });
});

app.MapPut("/api/config/system-prompt", async (
    IConfigurationService configService,
    IConversationManager conversationManager,
    SystemPromptRequest request) =>
{
    try
    {
        // Update configuration
        await configService.UpdateSystemPromptAsync(request.SystemPrompt);

        // Update conversation manager for hot-reload
        conversationManager.UpdateSystemPrompt(request.SystemPrompt);

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/config/agent-config", async (
    IConfigurationService configService,
    IConversationManager conversationManager,
    AgentConfigRequest request) =>
{
    try
    {
        // Update configuration
        await configService.UpdateAgentConfigAsync(request.ContextWindowSize);

        // Update conversation manager for hot-reload
        conversationManager.UpdateContextWindowSize(request.ContextWindowSize);

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/config/llm-parameters", async (
    IConfigurationService configService,
    LLMParametersRequest request) =>
{
    try
    {
        // Update configuration (this will be used for next LLM calls)
        await configService.UpdateLLMParametersAsync(
            request.Temperature,
            request.MaxTokens,
            request.TopP);

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

// Request DTOs
record SystemPromptRequest(string SystemPrompt);
record AgentConfigRequest(int ContextWindowSize);
record LLMParametersRequest(double Temperature, int MaxTokens, double TopP);
