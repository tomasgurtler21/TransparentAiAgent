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
using TransparentAiAgentCore.Domain.Memory;
using TransparentAiAgentCore.Infrastructure.Memory;
using TransparentAiAgentCore.Infrastructure.Tools.BuiltInLongTermMemory;
using TransparentAiAgentCore.Application.Localization;
using TransparentAiAgentCore.Infrastructure.Localization;
using TransparentAiAgentCore.Infrastructure.DataPath;

try
{
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
    builder.Services.AddSingleton<IDataPathService, DataPathService>();
    builder.Services.AddSingleton<IUserSettingsService, UserSettingsService>();
    builder.Services.AddSingleton<ITransparencyService, TransparencyService>();
    builder.Services.AddSingleton<ISerializationService, SerializationService>();
    builder.Services.AddSingleton<IToolUsageStatistics, ToolUsageStatistics>();
    builder.Services.AddSingleton<ToolSchemaValidator>();

    // Register Language service (singleton for app-wide state)
    builder.Services.AddSingleton<ILanguageService, LanguageService>();

    // Register Translation service (singleton for app-wide translation loading)
    builder.Services.AddSingleton<ITranslationService>(sp =>
    {
        var languageService = sp.GetRequiredService<ILanguageService>();
        var translationsPath = Path.Combine(builder.Environment.ContentRootPath, "data", "translations");
        var translationService = new TranslationService(translationsPath, languageService);

        // Load translations on startup
        translationService.LoadTranslationsAsync().Wait();

        return translationService;
    });

    // Register Conversation History services
    builder.Services.AddSingleton<MessageSerializer>();
    builder.Services.AddSingleton<IConversationRepository>(sp =>
    {
        var messageSerializer = sp.GetRequiredService<MessageSerializer>();
        var logger = sp.GetRequiredService<ILogger<JsonConversationRepository>>();
        var dataPathService = sp.GetRequiredService<IDataPathService>();
        return new JsonConversationRepository(messageSerializer, logger, dataPathService);
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
        var knowledgeBasePath = Path.Combine(builder.Environment.ContentRootPath, "data", "knowledge");
        return new JsonKnowledgeLibrary(knowledgeBasePath, logger);
    });
    builder.Services.AddSingleton<BuiltInKnowledgeToolRegistry>(sp =>
    {
        var knowledgeLibrary = sp.GetRequiredService<IKnowledgeLibrary>();
        return new BuiltInKnowledgeToolRegistry(knowledgeLibrary);
    });
    builder.Services.AddScoped<KnowledgeLibraryToolExecutor>();
    builder.Services.AddSingleton<TeachingModePromptBuilder>();

    // Register Tool services (if tools are enabled)
    if (appConfig.Tools.EnableTools)
    {
        // Register IToolRegistry first (needed by UI components like ToolsOverview)
        // IMPORTANT: Tool discovery will be triggered synchronously after app is built
        // to avoid race conditions where tools aren't available on first request
        builder.Services.AddSingleton<IToolRegistry>(sp =>
        {
            try
            {
                // Create MCP Tool Registry
                var mcpRegistry = new MCPToolRegistry(appConfig.Tools);

                // Create Built-in UI Control Tool Registry (Phase 9)
                var uiControlRegistry = sp.GetRequiredService<BuiltInUIControlToolRegistry>();

                // Create Built-in Knowledge Tool Registry (Phase 11)
                var knowledgeRegistry = sp.GetRequiredService<BuiltInKnowledgeToolRegistry>();

                // Create Built-in Long-Term Memory Tool Registry (Phase 4)
                var memoryRegistry = sp.GetRequiredService<BuiltInLongTermMemoryToolRegistry>();

                // Create Tool Registry Composite (MCP + UI Control + Knowledge + Memory)
                var compositeRegistry = new ToolRegistryComposite(new IToolRegistry[] { mcpRegistry, uiControlRegistry, knowledgeRegistry, memoryRegistry });

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

                // Create Long-Term Memory Tool Executor (Phase 4) - always available
                var memoryExecutor = sp.GetRequiredService<LongTermMemoryToolExecutor>();

                // Get tool usage statistics service
                var statistics = sp.GetRequiredService<IToolUsageStatistics>();

                // Get tool schema validator service (Phase 9b - Tool Execution Safety)
                var validator = sp.GetRequiredService<ToolSchemaValidator>();

                // Get app mode service for mode-aware tool filtering (Phase 9 - Teaching Mode)
                var appModeService = sp.GetRequiredService<IAppModeService>();

                // Build list of executors (UI Control + Knowledge + Memory always available)
                var executors = new List<IToolExecutor> { uiControlExecutor, knowledgeExecutor, memoryExecutor };

                // Add MCP executor only if MCP servers are configured
                if (appConfig.Tools.Servers.Count > 0)
                {
                    // Create MCP Tool Discovery
                    var mcpDiscovery = new MCPToolDiscovery(appConfig.Tools);

                    // Create MCP Tool Executor
                    var mcpExecutor = new MCPToolExecutor(mcpDiscovery);
                    executors.Add(mcpExecutor);

                    Console.WriteLine($"✓ Tool system enabled with {appConfig.Tools.Servers.Count} MCP server(s) + UI control tools + knowledge library tools + long-term memory tools");
                }
                else
                {
                    Console.WriteLine("✓ Tool system enabled with UI control tools + knowledge library tools + long-term memory tools (no MCP servers configured)");
                }

                // Create Tool Manager with all available executors and mode service for filtering
                // Note: UI control and knowledge library tools are only visible in Teaching mode
                // Note: Long-term memory tools are only visible when user has enabled memory in settings
                var userSettingsService = sp.GetService<IUserSettingsService>();
                var toolManager = new ToolManager(
                    toolRegistry,
                    executors,
                    transparencyService,
                    statistics,
                    validator,
                    appModeService,
                    userSettingsService);

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
        // Register new multi-provider system
        var llmConfig = appConfig.LLM;

        // Validate all provider configurations - skip invalid ones instead of crashing
        var validator = new TransparentAiAgentCore.Infrastructure.Configuration.ProviderConfigValidator();
        var validProviders = new Dictionary<string, ProviderConfig>();
        var invalidProviders = new List<(string Name, string[] Errors)>();

        if (llmConfig.Providers != null)
        {
            foreach (var (name, providerConfig) in llmConfig.Providers)
            {
                try
                {
                    var validationResult = validator.Validate(providerConfig);
                    if (validationResult.IsValid)
                    {
                        validProviders[name] = providerConfig;
                    }
                    else
                    {
                        invalidProviders.Add((name, validationResult.Errors.ToArray()));
                        Console.WriteLine($"⚠ Provider '{name}' has invalid configuration and will be skipped:");
                        foreach (var error in validationResult.Errors)
                        {
                            Console.WriteLine($"   - {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    invalidProviders.Add((name, new[] { ex.Message }));
                    Console.WriteLine($"⚠ Provider '{name}' validation failed and will be skipped: {ex.Message}");
                }
            }

            // Update llmConfig.Providers to only include valid providers
            llmConfig.Providers = validProviders;

            if (validProviders.Count > 0)
            {
                Console.WriteLine($"✓ Validated {validProviders.Count} provider configuration(s)");
                if (invalidProviders.Count > 0)
                {
                    Console.WriteLine($"  ({invalidProviders.Count} provider(s) skipped due to configuration errors)");
                }

                // If active provider is invalid, switch to first valid provider
                if (!string.IsNullOrEmpty(llmConfig.ActiveProvider) && !validProviders.ContainsKey(llmConfig.ActiveProvider))
                {
                    var firstValidProvider = validProviders.Keys.First();
                    Console.WriteLine($"⚠ Active provider '{llmConfig.ActiveProvider}' is invalid, switching to '{firstValidProvider}'");
                    llmConfig.ActiveProvider = firstValidProvider;
                }

                // Apply effective parameters (DefaultParameters merged with active provider's ParameterOverrides)
                if (!string.IsNullOrEmpty(llmConfig.ActiveProvider) && llmConfig.Providers.ContainsKey(llmConfig.ActiveProvider))
                {
                    var activeProviderConfig = llmConfig.Providers[llmConfig.ActiveProvider];
                    var effectiveParams = llmConfig.DefaultParameters != null && activeProviderConfig.ParameterOverrides != null
                        ? activeProviderConfig.ParameterOverrides.GetEffectiveParameters(llmConfig.DefaultParameters)
                        : activeProviderConfig.ParameterOverrides ?? llmConfig.DefaultParameters ?? new ProviderParameters();

                    // Apply effective parameters to internal properties used by orchestrator
                    llmConfig.Temperature = effectiveParams.Temperature;
                    llmConfig.TopP = effectiveParams.TopP;
                    llmConfig.MaxTokens = effectiveParams.MaxTokens ?? 4096;
                    llmConfig.UseRest = effectiveParams.UseRest ?? false;
                }
            }
            else
            {
                Console.WriteLine($"⚠ No valid LLM providers configured ({invalidProviders.Count} provider(s) had configuration errors)");
                Console.WriteLine("  The app will start but LLM features will not be available.");
                isLLMConfigured = false;
            }
        }

        // Only register LLM services if we have valid providers
        if (isLLMConfigured && llmConfig.Providers != null && llmConfig.Providers.Count > 0)
        {
            // Register LLM services
            builder.Services.AddSingleton(llmConfig);
        builder.Services.AddSingleton<TransparentAiAgentCore.Domain.LLM.ILLMProviderFactory, LLMProviderFactory>();
        builder.Services.AddSingleton<LLMProviderFactory>(sp =>
            sp.GetRequiredService<TransparentAiAgentCore.Domain.LLM.ILLMProviderFactory>() as LLMProviderFactory
            ?? throw new InvalidOperationException("LLMProviderFactory not registered"));

        // Register Provider Manager (new multi-provider system)
        if (llmConfig.Providers != null && llmConfig.Providers.Count > 0)
        {
            builder.Services.AddSingleton<TransparentAiAgentCore.Domain.LLM.ILLMProviderManager, TransparentAiAgentCore.Infrastructure.LLM.LLMProviderManager>();
            Console.WriteLine($"✓ Multi-provider system enabled with {llmConfig.Providers.Count} provider(s)");

            // Register ProviderStateService (depends on ILLMProviderManager)
            builder.Services.AddScoped<IProviderStateService, ProviderStateService>();

            // Also register ILLMProvider for backward compatibility (delegates to active provider)
            // Uses DelegatingLLMProvider to ensure current active provider is used on every request
            builder.Services.AddSingleton<ILLMProvider>(sp =>
            {
                var manager = sp.GetRequiredService<TransparentAiAgentCore.Domain.LLM.ILLMProviderManager>();
                return new DelegatingLLMProvider(manager);
            });
        }

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

            var activeProvider = llmConfig.ActiveProvider ?? llmConfig.Provider ?? "Unknown";
            Console.WriteLine($"✓ Active LLM Provider: {activeProvider}");
        }
        else
        {
            // No valid providers - register NotConfiguredAgentOrchestrator
            builder.Services.AddScoped<IAgentOrchestrator>(sp =>
            {
                var conversationManager = sp.GetRequiredService<IConversationManager>();
                return new NotConfiguredAgentOrchestrator(conversationManager, "No valid LLM providers configured. All providers had configuration errors.");
            });

            Console.WriteLine("⚠ No valid providers. Agent will show configuration error when used.");
            Console.WriteLine("  Configure LLM settings in appsettings.json to enable agent features.");
            Console.WriteLine("  See docs/CONFIGURATION_SETUP.md for instructions.");
        }
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
    // Note: IProviderStateService is registered conditionally with ILLMProviderManager (see line ~264)

    // Register UI Control services (Phase 9 - Teaching Mode)
    builder.Services.AddSingleton<IUIControlService, UIControlService>();  // Singleton to share across all render contexts
    builder.Services.AddSingleton<BuiltInUIControlToolRegistry>();
    builder.Services.AddScoped<UIControlToolExecutor>();  // Scoped to work with scoped IToolManager

    // Register App Mode service (Phase 9d - Teaching Mode System)
    builder.Services.AddScoped<IAppModeService, AppModeService>();  // Scoped to match ConversationManager lifetime

    // Register Long-Term Memory services (Phase 4 - Long-Term Memory)
    // Load memory configuration from appsettings
    var memoryConfig = new LongTermMemoryConfiguration();
    builder.Configuration.GetSection("TransparentAiAgent:LongTermMemory").Bind(memoryConfig);
    builder.Services.AddSingleton(memoryConfig);

    // Register memory service
    builder.Services.AddScoped<ILongTermMemoryService>(sp =>
    {
        var config = sp.GetRequiredService<LongTermMemoryConfiguration>();
        var dataPathService = sp.GetRequiredService<IDataPathService>();
        var logger = sp.GetRequiredService<ILogger<LongTermMemoryService>>();
        return new LongTermMemoryService(config, dataPathService, logger);
    });

    // Register memory tools
    builder.Services.AddSingleton<BuiltInLongTermMemoryToolRegistry>();
    builder.Services.AddScoped<LongTermMemoryToolExecutor>();

    // Register Scenario services (Phase 10a/10b - Teaching Mode Scenarios)
    // Register JsonScenarioLoader first (needed by ScenarioRegistry)
    builder.Services.AddSingleton<JsonScenarioLoader>(sp =>
    {
        var translationService = sp.GetRequiredService<ITranslationService>();
        var logger = sp.GetRequiredService<ILogger<JsonScenarioLoader>>();
        return new JsonScenarioLoader(translationService, logger);
    });

    // Register ScenarioRegistry with automatic scenario loading and language change handling
    builder.Services.AddSingleton<IScenarioRegistry>(sp =>
    {
        var loader = sp.GetRequiredService<JsonScenarioLoader>();
        var languageService = sp.GetRequiredService<ILanguageService>();
        var translationService = sp.GetRequiredService<ITranslationService>();
        var logger = sp.GetRequiredService<ILogger<ScenarioRegistry>>();
        var scenariosPath = Path.Combine(builder.Environment.ContentRootPath, "data", "scenarios");
        return new ScenarioRegistry(loader, languageService, translationService, logger, scenariosPath);
    });

    builder.Services.AddScoped<IConfigurationOverlay>(sp =>
    {
        var appConfig = sp.GetRequiredService<AppConfiguration>();

        // Populate base configuration with values from appsettings.json
        var baseConfig = new Dictionary<string, object>
        {
            { "messageLimit", appConfig.Agent.ContextWindowSize },
            { "systemPrompt", appConfig.Agent.SystemPrompt ?? string.Empty },
            { "enableTools", appConfig.Tools.EnableTools },
            { "toolExecutionMode", appConfig.Tools.ToolExecutionMode.ToString() },
            // Future-proof: Add other config values as needed
        };

        return new ConfigurationOverlayService(baseConfig);
    });
    builder.Services.AddScoped<IConditionEvaluator, ConditionEvaluator>();
    builder.Services.AddScoped<IScenarioExecutor, ScenarioExecutor>();

    // Register HttpClient for API calls
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

    var app = builder.Build();

    // Display loaded scenarios (Phase 10a - Teaching Mode Scenarios)
    // Note: Scenarios are now loaded automatically by ScenarioRegistry constructor
    try
    {
        var scenarioRegistry = app.Services.GetRequiredService<IScenarioRegistry>();
        var scenarios = scenarioRegistry.GetAllScenarios();

        if (scenarios.Count > 0)
        {
            Console.WriteLine($"✓ Loaded {scenarios.Count} teaching scenario(s)");
            Console.WriteLine("   Scenarios:");
            foreach (var scenario in scenarios.OrderBy(s => s.Name))
            {
                Console.WriteLine($"     - {scenario.Name} ({scenario.Steps.Count} steps)");
            }
        }
        else
        {
            var scenariosPath = Path.Combine(builder.Environment.ContentRootPath, "data", "scenarios");
            Console.WriteLine($"⚠ No teaching scenarios loaded from {scenariosPath}");
            Console.WriteLine("   Check that scenario files exist and are valid JSON.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Failed to access teaching scenarios: {ex.Message}");
        Console.WriteLine("   The app will start but teaching scenarios may not be available.");
    }

    // CRITICAL FIX: Discover tools synchronously BEFORE accepting requests
    // This prevents race condition where tools aren't available on first request
    if (appConfig.Tools.EnableTools && appConfig.Tools.AutoDiscoverTools && appConfig.Tools.Servers.Count > 0)
    {
        try
        {
            Console.WriteLine($"⏳ Discovering tools from {appConfig.Tools.Servers.Count} MCP server(s)...");
            Console.WriteLine($"   MCP Servers: {string.Join(", ", appConfig.Tools.Servers.Select(s => s.Name))}");

            var toolRegistry = app.Services.GetRequiredService<IToolRegistry>();
            Console.WriteLine($"   Tool registry type: {toolRegistry.GetType().Name}");

            await toolRegistry.RefreshAsync();

            var tools = toolRegistry.GetAllTools();
            Console.WriteLine($"✓ Discovered {tools.Count} tools from {appConfig.Tools.Servers.Count} MCP server(s)");

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
        Console.WriteLine($"   EnableTools: {appConfig.Tools.EnableTools}");
        Console.WriteLine($"   AutoDiscoverTools: {appConfig.Tools.AutoDiscoverTools}");
        Console.WriteLine($"   MCP Servers count: {appConfig.Tools.Servers.Count}");
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

    // Auto-open browser on startup
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            // Get the URL from configuration or use default
            var urls = app.Urls;
            var url = urls.FirstOrDefault() ?? "http://localhost:5000";

            // Open browser on Windows, macOS, or Linux
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);

            Console.WriteLine($"🌐 Opening browser at {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Could not open browser automatically: {ex.Message}");
        }
    });

    // Configuration API endpoints
    app.MapGet("/api/config", (IConfigurationService configService) =>
    {
        var config = configService.GetConfiguration();
        return Results.Ok(new
        {
            Agent = new
            {
                config.Agent.SystemPrompt,
                config.Agent.ContextWindowSize
            },
            LLM = new
            {
                config.LLM.Provider,
                config.LLM.Temperature,
                config.LLM.MaxTokens,
                config.LLM.TopP
            },
            Tools = new
            {
                config.Tools.EnableTools,
                config.Tools.ToolExecutionMode
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
}
catch (Exception ex)
{
    // Unhandled exception during startup - write crash log
    var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
    Directory.CreateDirectory(logsDir);

    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
    var crashLogPath = Path.Combine(logsDir, $"crash_{timestamp}.txt");

    var crashLog = $@"TRANSPARENT AI AGENT - CRASH LOG
=====================================
Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
Exception Type: {ex.GetType().FullName}
Message: {ex.Message}

Stack Trace:
{ex.StackTrace}

Inner Exception:
{(ex.InnerException != null ? $@"Type: {ex.InnerException.GetType().FullName}
Message: {ex.InnerException.Message}
Stack Trace:
{ex.InnerException.StackTrace}" : "None")}

Environment:
- OS: {Environment.OSVersion}
- .NET: {Environment.Version}
- Working Directory: {Environment.CurrentDirectory}

Additional Details:
{ex}
=====================================
";

    File.WriteAllText(crashLogPath, crashLog);

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("FATAL ERROR: Application crashed during startup");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine($"A crash log has been written to: {crashLogPath}");
    Console.WriteLine();
    Console.WriteLine("Common causes:");
    Console.WriteLine("  - Invalid JSON syntax in appsettings.json");
    Console.WriteLine("  - Missing or corrupted configuration files");
    Console.WriteLine("  - Permission issues accessing files or directories");
    Console.WriteLine();
    Console.WriteLine("Please check the crash log for detailed information.");
    Console.WriteLine();

    Environment.Exit(1);
}

// Request DTOs
record SystemPromptRequest(string SystemPrompt);
record AgentConfigRequest(int ContextWindowSize);
record LLMParametersRequest(double Temperature, int MaxTokens, double TopP);
