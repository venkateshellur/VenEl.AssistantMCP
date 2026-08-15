using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.MSSql.Configuration;
using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.MSSql.Services;

/// <inheritdoc />
internal sealed class SqlConnectionFactory(IOptions<MSSqlOptions> options, SecretManager secretManager) : ISqlConnectionFactory
{
    private readonly MSSqlOptions _options = options.Value;
    private readonly SecretManager _secretManager = secretManager;

    // ── ISqlConnectionFactory ─────────────────────────────────────────────────

    public IReadOnlyList<SqlServerEntry> GetAllServers() => _options.Servers.AsReadOnly();

    public SqlServerEntry? FindServer(string serverName) =>
        _options.Servers.FirstOrDefault(
            s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));

    public async Task<SqlConnection> CreateFromServerNameAsync(string serverName, string? database = null, CancellationToken ct = default)
    {
        var entry = FindServer(serverName)
            ?? throw new InvalidOperationException(
                $"No configured server named '{serverName}'. " +
                $"Available: {string.Join(", ", _options.Servers.Select(s => s.Name))}");

        var effectiveDatabase = database ?? entry.DefaultDatabase;
        return await BuildAsync(entry.ConnectionString, effectiveDatabase, ct);
    }

    public async Task<SqlConnection> CreateFromConnectionStringAsync(string connectionString, string? database = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        return await BuildAsync(connectionString, database, ct);
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

    private async Task<SqlConnection> BuildAsync(string baseConnectionString, string? database, CancellationToken ct)
    {
        var resolvedConnectionString = await _secretManager.ResolveSecretAsync(baseConnectionString, ct);
        var csb = new SqlConnectionStringBuilder(resolvedConnectionString);

        if (!string.IsNullOrWhiteSpace(database))
            csb.InitialCatalog = database;

        return new SqlConnection(csb.ConnectionString);
    }
}
