using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenEl.MCPAssistant.Core.Registration;
using VenEl.MCPAssistant.Atlassian.Extensions;
using VenEl.MCPAssistant.Azure.Extensions;
using VenEl.MCPAssistant.GitHub.Extensions;
using VenEl.MCPAssistant.MSSql.Extensions;
using VenEl.MCPAssistant.Logging.Extensions;
using VenEl.MCPAssistant.Docker.Extensions;

// ─────────────────────────────────────────────────────────────────────────────
// VenEl MCP Assistant – STDIO MCP Server
//
// This file is intentionally thin: it only wires the infrastructure together.
// No tool types are referenced here directly.
//
// To add a new feature (GitHub, Azure, AWS, Atlassian, …):
//   1. Create its class library and implement the feature.
//   2. Add one line below in the "Feature Modules" section.
//   3. That's it — the feature self-registers its MCP tools automatically.
// ─────────────────────────────────────────────────────────────────────────────

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// ── Configuration ─────────────────────────────────────────────────────────────
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: "VENEL_");

// ── Logging ───────────────────────────────────────────────────────────────────
// All log output goes to stderr so the STDIO JSON-RPC stream stays clean.
builder.Logging
    .ClearProviders()
    .SetMinimumLevel(LogLevel.Warning);

// ══ Feature Modules ══════════════════════════════════════════════════════════
// Each feature self-registers its DI services AND its MCP tools into the
// shared McpFeatureRegistry. Program.cs never references tool types directly.
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddMSSqlFeature(builder.Configuration);
builder.Services.AddAtlassianFeature(builder.Configuration);
builder.Services.AddAzureFeature(builder.Configuration);
builder.Services.AddGitHubFeature();
builder.Services.AddDockerFeature();
builder.Services.AddLoggingFeature(builder.Configuration);

// Add future features below — one line each, fully independent:
// builder.Services.AddAzureFeature(builder.Configuration);
// builder.Services.AddAwsFeature(builder.Configuration);

// ═════════════════════════════════════════════════════════════════════════════

// ── MCP Server ────────────────────────────────────────────────────────────────
var mcpBuilder = builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name    = "VenEl.MCPAssistant",
            Version = "1.0.0"
        };
    })
    .WithStdioServerTransport();

// Dynamically apply every registered feature's MCP tools in one shot.
// Parse optional --feature or -f arguments (e.g. --feature azure --feature mssql)
var allowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < args.Length; i++)
{
    if ((args[i] == "--feature" || args[i] == "-f") && i + 1 < args.Length)
    {
        allowedFeatures.Add(args[i + 1]);
        i++;
    }
}

builder.Services
    .GetOrAddFeatureRegistry()
    .ApplyAll(mcpBuilder, allowedFeatures);

// ── Run ───────────────────────────────────────────────────────────────────────
var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogWarning("VenEl MCP Assistant server started successfully.");
await host.RunAsync();
