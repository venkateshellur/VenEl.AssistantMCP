using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.MSSql.Configuration;

namespace VenEl.AssistantMCP.MSSql.Services;

/// <inheritdoc />
internal sealed class SqlConnectionFactory(IOptions<MSSqlOptions> options) : ISqlConnectionFactory
{
    private readonly MSSqlOptions _options = options.Value;

    // ── ISqlConnectionFactory ─────────────────────────────────────────────────

    public IReadOnlyList<SqlServerEntry> GetAllServers() => _options.Servers.AsReadOnly();

    public SqlServerEntry? FindServer(string serverName) =>
        _options.Servers.FirstOrDefault(
            s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));

    public SqlConnection CreateFromServerName(string serverName, string? database = null)
    {
        var entry = FindServer(serverName)
            ?? throw new InvalidOperationException(
                $"No configured server named '{serverName}'. " +
                $"Available: {string.Join(", ", _options.Servers.Select(s => s.Name))}");

        var effectiveDatabase = database ?? entry.DefaultDatabase;
        return Build(entry.ConnectionString, effectiveDatabase);
    }

    public SqlConnection CreateFromConnectionString(string connectionString, string? database = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        return Build(connectionString, database);
    }

    public bool IsDestructiveAllowed(string? serverName = null)
    {
        if (serverName is not null)
        {
            var entry = FindServer(serverName);
            if (entry?.AllowDestructiveOperations is bool perServer)
                return perServer;
        }
        return _options.AllowDestructiveOperations;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SqlConnection Build(string baseConnectionString, string? database)
    {
        var csb = new SqlConnectionStringBuilder(baseConnectionString);

        if (!string.IsNullOrWhiteSpace(database))
            csb.InitialCatalog = database;

        return new SqlConnection(csb.ConnectionString);
    }
}
