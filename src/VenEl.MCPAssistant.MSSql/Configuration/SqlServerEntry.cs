namespace VenEl.MCPAssistant.MSSql.Configuration;

/// <summary>
/// Represents a single named SQL Server connection entry in configuration.
/// </summary>
public sealed class SqlServerEntry
{
    /// <summary>
    /// Friendly, unique name used to reference this server in MCP tool calls.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full ADO.NET connection string.  The <c>Initial Catalog</c> / <c>Database</c>
    /// can be left out here and supplied at call time.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional default database.  Used when no database is specified in a tool call.
    /// </summary>
    public string? DefaultDatabase { get; set; }

    /// <summary>
    /// Human-readable description surfaced in the <c>sql_list_configured_servers</c> tool.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Per-server override for destructive operations.
    /// <c>null</c> means "inherit the global <see cref="MSSqlOptions.AllowDestructiveOperations"/> setting".
    /// </summary>
    public bool? AllowDestructiveOperations { get; set; }
}
