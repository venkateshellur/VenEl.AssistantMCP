using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VenEl.AssistantMCP.Core.Registration;
using VenEl.AssistantMCP.MSSql.Configuration;
using VenEl.AssistantMCP.MSSql.Services;
using VenEl.AssistantMCP.MSSql.Tools;

namespace VenEl.AssistantMCP.MSSql.Extensions;

/// <summary>
/// DI registration extensions for the MSSql feature module.
/// </summary>
public static class MSSqlServiceExtensions
{
    /// <summary>
    /// Registers all MSSql services and self-registers the MSSql MCP tools
    /// into the shared <see cref="McpFeatureRegistry"/>.
    ///
    /// <para>
    /// In <c>Program.cs</c>, add one line per feature module:
    /// <code>
    ///     builder.Services.AddMSSqlFeature(builder.Configuration);
    ///     // builder.Services.AddGitHubFeature(builder.Configuration);
    ///     // builder.Services.AddAwsFeature(builder.Configuration);
    /// </code>
    /// Then apply them all to the MCP builder in one call:
    /// <code>
    ///     builder.Services.GetOrAddFeatureRegistry().ApplyAll(mcpBuilder);
    /// </code>
    /// </para>
    /// </summary>
    public static IServiceCollection AddMSSqlFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DI services ──────────────────────────────────────────────────────
        services.Configure<MSSqlOptions>(
            configuration.GetSection(MSSqlOptions.SectionName));

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        // ── Action Handlers ───────────────────────────────────────────────────
        services.AddActionHandlersFromAssembly<MSSqlCommandArgs>(typeof(MSSqlServiceExtensions).Assembly);

        // ── Self-register MCP tools into the shared registry ─────────────────
        // Program.cs never needs to reference MSSqlTools directly.
        services.GetOrAddFeatureRegistry().Register(
            featureName:      "MSSql",
            description:      "Microsoft SQL Server tools: query, schema inspection, " +
                              "stored procedures, and (optionally) data modification.",
            toolRegistration: mcpBuilder => mcpBuilder.WithTools<MSSqlDispatcherTool>());

        return services;
    }
}
