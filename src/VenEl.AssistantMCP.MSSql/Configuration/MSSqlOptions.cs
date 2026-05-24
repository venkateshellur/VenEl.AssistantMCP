namespace VenEl.AssistantMCP.MSSql.Configuration;

/// <summary>
/// Top-level options for the MSSql feature, bound from the <c>MSSql</c> configuration section.
/// </summary>
public sealed class MSSqlOptions
{
    /// <summary>The configuration section key.</summary>
    public const string SectionName = "MSSql";

    /// <summary>
    /// Global switch that controls whether DELETE / TRUNCATE / DROP / UPDATE statements
    /// are permitted across ALL configured servers.
    /// <para>
    /// Defaults to <c>false</c> (safe mode).  Individual servers can override this via
    /// <see cref="SqlServerEntry.AllowDestructiveOperations"/>.
    /// </para>
    /// </summary>
    public bool AllowDestructiveOperations { get; set; } = false;

    /// <summary>
    /// Named SQL Server connection entries loaded from configuration.
    /// Additional servers can be targeted at call-time by supplying an ad-hoc
    /// connection string directly in the MCP tool call.
    /// </summary>
    public List<SqlServerEntry> Servers { get; set; } = [];
}
