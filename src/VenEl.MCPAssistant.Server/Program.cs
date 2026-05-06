using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenEl.MCPAssistant.Core.Registration;
using VenEl.MCPAssistant.MSSql.Extensions;

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
    .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace)
    .SetMinimumLevel(LogLevel.Warning);

// ══ Feature Modules ══════════════════════════════════════════════════════════
// Each feature self-registers its DI services AND its MCP tools into the
// shared McpFeatureRegistry. Program.cs never references tool types directly.
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddMSSqlFeature(builder.Configuration);

// Add future features below — one line each, fully independent:
// builder.Services.AddGitHubFeature(builder.Configuration);
// builder.Services.AddAtlassianFeature(builder.Configuration);
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
builder.Services
    .GetOrAddFeatureRegistry()
    .ApplyAll(mcpBuilder);

// ── Run ───────────────────────────────────────────────────────────────────────
var host = builder.Build();
await host.RunAsync();
