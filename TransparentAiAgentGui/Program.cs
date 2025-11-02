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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Core Infrastructure services
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<ITransparencyService, TransparencyService>();
builder.Services.AddSingleton<ISerializationService, SerializationService>();
builder.Services.AddSingleton<IToolUsageStatistics, ToolUsageStatistics>();

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

// Register Conversation Manager (always needed)
builder.Services.AddSingleton<IConversationManager>(sp =>
{
    var config = sp.GetRequiredService<AppConfiguration>();
    var transparencyService = sp.GetRequiredService<ITransparencyService>();
    return new ConversationManager(config.Agent.ContextWindowSize, transparencyService);
});

// Register Tool services (if tools are enabled)
if (appConfig.Agent.EnableTools)
{
    // Register IToolRegistry first (needed by UI components like ToolsOverview)
    builder.Services.AddSingleton<IToolRegistry>(sp =>
    {
        try
        {
            // Create MCP Tool Registry
            var mcpRegistry = new MCPToolRegistry(appConfig.MCP);

            // Create Tool Registry Composite (for now just MCP, can add built-in tools later)
            var compositeRegistry = new ToolRegistryComposite(new[] { mcpRegistry });

            // Discover tools on startup if configured (and if servers exist)
            if (appConfig.MCP.AutoDiscoverTools && appConfig.MCP.Servers.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await compositeRegistry.RefreshAsync();
                        var tools = compositeRegistry.GetAllTools();
                        Console.WriteLine($"✓ Discovered {tools.Count} tools from {appConfig.MCP.Servers.Count} MCP server(s)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Tool discovery failed: {ex.Message}");
                    }
                });
            }

            return compositeRegistry;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Failed to initialize tool registry: {ex.Message}");
            throw;
        }
    });

    // Register IToolManager only if we have servers configured
    if (appConfig.MCP.Servers.Count > 0)
    {
        builder.Services.AddSingleton<IToolManager>(sp =>
        {
            try
            {
                var transparencyService = sp.GetRequiredService<ITransparencyService>();
                var toolRegistry = sp.GetRequiredService<IToolRegistry>();

                // Create MCP Tool Discovery
                var mcpDiscovery = new MCPToolDiscovery(appConfig.MCP);

                // Create MCP Tool Executor
                var mcpExecutor = new MCPToolExecutor(mcpDiscovery);

                // Get tool usage statistics service
                var statistics = sp.GetRequiredService<IToolUsageStatistics>();

                // Create Tool Manager
                var toolManager = new ToolManager(
                    toolRegistry,
                    new IToolExecutor[] { mcpExecutor },
                    transparencyService,
                    statistics);

                Console.WriteLine($"✓ Tool system enabled with {appConfig.MCP.Servers.Count} MCP server(s)");
                return toolManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Failed to initialize tool manager: {ex.Message}");
                throw;
            }
        });
    }
    else
    {
        Console.WriteLine("⚠ Tools enabled but no MCP servers configured - IToolRegistry available but no IToolManager");
    }
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
    builder.Services.AddSingleton<IAgentOrchestrator>(sp =>
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
    // Register stub implementation that throws helpful errors with details
    builder.Services.AddSingleton<IAgentOrchestrator>(sp =>
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

// Register HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

var app = builder.Build();

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
record LLMParametersRequest(double Temperature, int MaxTokens, double TopP);
