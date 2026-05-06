using Microsoft.Data.SqlClient;
using VenEl.MCPAssistant.MSSql.Configuration;

namespace VenEl.MCPAssistant.MSSql.Services;

/// <summary>
/// Creates and configures <see cref="SqlConnection"/> instances from either
/// named configured servers or ad-hoc connection strings.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Returns all named servers loaded from configuration.
    /// </summary>
    IReadOnlyList<SqlServerEntry> GetAllServers();

    /// <summary>
    /// Looks up a server entry by its <see cref="SqlServerEntry.Name"/> (case-insensitive).
    /// Returns <c>null</c> if not found.
    /// </summary>
    SqlServerEntry? FindServer(string serverName);

    /// <summary>
    /// Creates a <see cref="SqlConnection"/> for a named configured server,
    /// optionally overriding the database.
    /// </summary>
    /// <param name="serverName">The <see cref="SqlServerEntry.Name"/> value.</param>
    /// <param name="database">Optional database override (overrides <c>InitialCatalog</c>).</param>
    SqlConnection CreateFromServerName(string serverName, string? database = null);

    /// <summary>
    /// Creates a <see cref="SqlConnection"/> from a raw connection string,
    /// optionally overriding the database.
    /// </summary>
    SqlConnection CreateFromConnectionString(string connectionString, string? database = null);

    /// <summary>
    /// Resolves whether destructive operations are permitted for a given server.
    /// Per-server setting takes precedence over the global setting.
    /// </summary>
    /// <param name="serverName">Named server (nullable — falls back to global setting).</param>
    bool IsDestructiveAllowed(string? serverName = null);
}
