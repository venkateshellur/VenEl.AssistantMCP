using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Registration;
using VenEl.MCP.Core.Extensions;
using VenEl.MCP.Atlassian.Extensions;
using VenEl.MCP.Azure.Extensions;
using VenEl.MCP.GitHub.Extensions;
using VenEl.MCP.MSSql.Extensions;
using VenEl.MCP.WebAutomator.Extensions;
using VenEl.MCP.Logging.Extensions;
using VenEl.MCP.Docker.Extensions;
using VenEl.MCP.LocalOffice.Extensions;
using VenEl.MCP.Slack.Extensions;
using VenEl.MCP.Kubernetes.Extensions;
using VenEl.MCP.AWS.Extensions;
using VenEl.MCP.GCP.Extensions;
using VenEl.MCP.Databricks.Extensions;
using VenEl.MCP.Bitwarden.Extensions;
using VenEl.MCP.MicrosoftTeams.Extensions;
using VenEl.MCP.Email.Extensions;
using VenEl.MCP.Host.Extensions;
using VenEl.MCP.FTP.Extensions;
using VenEl.MCP.ServiceNow.Extensions;
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
var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var userConfigDir = Path.Combine(userProfile, ".venel.assistant.mcp");
var userConfigPath = Path.Combine(userConfigDir, "appsettings.json");

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile(userConfigPath, optional: true, reloadOnChange: true)
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

builder.Services.AddCoreSecurity();
builder.Services.AddMSSqlFeature(builder.Configuration);
builder.Services.AddAtlassianFeature(builder.Configuration);
builder.Services.AddAzureFeature(builder.Configuration);
builder.Services.AddGitHubFeature(builder.Configuration);
builder.Services.AddDockerFeature(builder.Configuration);
builder.Services.AddLoggingFeature(builder.Configuration);
builder.Services.AddLocalOfficeTools(builder.Configuration);
builder.Services.AddSlackFeature(builder.Configuration);
builder.Services.AddKubernetesFeature(builder.Configuration);
builder.Services.AddAwsFeature(builder.Configuration);
builder.Services.AddGcpFeature(builder.Configuration);
builder.Services.AddDatabricksFeature(builder.Configuration);
builder.Services.AddBitwardenFeature(builder.Configuration);
builder.Services.AddTeamsMcp(builder.Configuration.GetSection("Teams"));
builder.Services.AddEmailFeature(builder.Configuration);
builder.Services.AddWebAutomator();
builder.Services.AddHostFeature(builder.Configuration);
builder.Services.AddFtpFeature();
builder.Services.AddServiceNowFeature(builder.Configuration);
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
            Name    = "VenEl.MCP",
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
