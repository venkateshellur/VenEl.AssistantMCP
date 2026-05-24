using Microsoft.Extensions.DependencyInjection;

namespace VenEl.AssistantMCP.Core.Registration;

/// <summary>
/// Central registry that collects tool-registration callbacks from every
/// feature module (MSSql, GitHub, Azure, …) at startup.
///
/// <para>
/// Each feature library calls <see cref="Register"/> during its DI setup.
/// The Server's <c>Program.cs</c> then calls <see cref="ApplyAll"/> once
/// to wire every collected registration into the MCP server builder —
/// without knowing anything about the individual tool types.
/// </para>
/// </summary>
public sealed class McpFeatureRegistry
{
    private readonly List<FeatureRegistration> _registrations = [];

    /// <summary>
    /// Enumerates all registered feature descriptions (name + description).
    /// Useful for diagnostics / logging at startup.
    /// </summary>
    public IReadOnlyList<FeatureRegistration> Registrations => _registrations.AsReadOnly();

    /// <summary>
    /// Called by a feature module during its DI setup to declare its MCP tools.
    /// </summary>
    /// <param name="featureName">Human-readable name, e.g. "MSSql".</param>
    /// <param name="description">Short description of what the feature provides.</param>
    /// <param name="toolRegistration">
    /// Callback that calls <c>.WithTools&lt;T&gt;()</c> (or equivalent) on the MCP builder.
    /// </param>
    public void Register(
        string featureName,
        string description,
        Action<IMcpServerBuilder> toolRegistration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
        ArgumentNullException.ThrowIfNull(toolRegistration);

        _registrations.Add(new FeatureRegistration(featureName, description, toolRegistration));
    }

    /// <summary>
    /// Applies every registered tool-registration callback to <paramref name="mcpBuilder"/>.
    /// Call this once in <c>Program.cs</c>, after all features have been added.
    /// </summary>
    /// <param name="mcpBuilder">The MCP server builder.</param>
    /// <param name="allowedFeatures">Optional set of feature names to apply. If null or empty, all features are applied.</param>
    public IMcpServerBuilder ApplyAll(IMcpServerBuilder mcpBuilder, IReadOnlySet<string>? allowedFeatures = null)
    {
        foreach (var reg in _registrations)
        {
            if (allowedFeatures == null || allowedFeatures.Count == 0 || allowedFeatures.Contains(reg.FeatureName, StringComparer.OrdinalIgnoreCase))
            {
                reg.ToolRegistration(mcpBuilder);
            }
        }

        return mcpBuilder;
    }
}

/// <summary>Describes a single registered feature module.</summary>
/// <param name="FeatureName">Human-readable name, e.g. "MSSql".</param>
/// <param name="Description">Short description of what the feature provides.</param>
/// <param name="ToolRegistration">The callback that registers this feature's MCP tools.</param>
public sealed record FeatureRegistration(
    string FeatureName,
    string Description,
    Action<IMcpServerBuilder> ToolRegistration);
