using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.MSSql.Guards;
using VenEl.MCPAssistant.MSSql.Services;

namespace VenEl.MCPAssistant.MSSql.Tools;

/// <summary>
/// MCP tools for interacting with Microsoft SQL Server instances.
/// All tools support either a named configured server or an ad-hoc connection string.
/// Destructive operations (DELETE / TRUNCATE / DROP / UPDATE) are blocked by default.
/// </summary>
[McpServerToolType]
public sealed class MSSqlTools(ISqlConnectionFactory factory, ILogger<MSSqlTools> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    // ═════════════════════════════════════════════════════════════════════════
    // Discovery & Metadata
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "sql_list_configured_servers")]
    [Description(
        "Lists all SQL Server connections that are pre-configured in appsettings.json. " +
        "Returns each server's name, description, default database, and whether " +
        "destructive operations are enabled for it.")]
    public string SqlListConfiguredServers()
    {
        var servers = factory.GetAllServers();

        if (servers.Count == 0)
            return "No servers are currently configured in appsettings.json under MSSql:Servers.";

        var result = servers.Select(s => new
        {
            s.Name,
            s.Description,
            s.DefaultDatabase,
            DestructiveOperationsAllowed = factory.IsDestructiveAllowed(s.Name)
        });

        return JsonSerializer.Serialize(result, JsonOpts);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sql_list_databases")]
    [Description(
        "Lists all databases on a SQL Server instance. " +
        "Supply either a pre-configured 'serverName' OR a raw 'connectionString'. " +
        "If both are provided, 'serverName' takes precedence.")]
    public async Task<string> SqlListDatabasesAsync(
        [Description("Name of a configured server (from sql_list_configured_servers).")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT name, state_desc AS [Status], create_date AS [CreatedAt]
            FROM   sys.databases
            WHERE  name NOT IN ('master','tempdb','model','msdb')
            ORDER  BY name
            """;

        await using var conn = ResolveConnection(serverName, connectionString, database: null);
        await conn.OpenAsync(cancellationToken);
        return await ExecuteReaderAsync(conn, sql, [], cancellationToken: cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sql_list_tables")]
    [Description(
        "Lists all user tables in a database. " +
        "Supply either 'serverName' (configured) or 'connectionString' (ad-hoc). " +
        "'database' overrides the connection's default catalog.")]
    public async Task<string> SqlListTablesAsync(
        [Description("Target database name. Overrides the connection's default database.")]
        string? database = null,
        [Description("Name of a configured server.")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        [Description("Filter by schema name (default: all schemas).")]
        string? schema = null,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT TABLE_SCHEMA  AS [Schema],
                   TABLE_NAME    AS [Table],
                   TABLE_TYPE    AS [Type]
            FROM   INFORMATION_SCHEMA.TABLES
            WHERE  TABLE_TYPE = 'BASE TABLE'
            """ +
            (schema is not null ? " AND TABLE_SCHEMA = @schema" : string.Empty) +
            " ORDER BY TABLE_SCHEMA, TABLE_NAME";

        var parameters = schema is not null
            ? new[] { new SqlParameter("@schema", schema) }
            : [];

        await using var conn = ResolveConnection(serverName, connectionString, database);
        await conn.OpenAsync(cancellationToken);
        return await ExecuteReaderAsync(conn, sql, parameters, cancellationToken: cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sql_describe_table")]
    [Description(
        "Returns the full column schema for a table, including data types, " +
        "nullability, default values, and primary key membership.")]
    public async Task<string> SqlDescribeTableAsync(
        [Description("Table name (without schema prefix).")]
        string tableName,
        [Description("Schema name (default: dbo).")]
        string schema = "dbo",
        [Description("Target database name. Overrides the connection's default database.")]
        string? database = null,
        [Description("Name of a configured server.")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.COLUMN_NAME          AS [Column],
                c.DATA_TYPE            AS [DataType],
                c.CHARACTER_MAXIMUM_LENGTH AS [MaxLength],
                c.NUMERIC_PRECISION    AS [Precision],
                c.NUMERIC_SCALE        AS [Scale],
                c.IS_NULLABLE          AS [Nullable],
                c.COLUMN_DEFAULT       AS [Default],
                c.ORDINAL_POSITION     AS [Position],
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'YES' ELSE 'NO' END AS [PrimaryKey]
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN   INFORMATION_SCHEMA.KEY_COLUMN_USAGE  ku
                       ON  tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                       AND tc.TABLE_SCHEMA    = ku.TABLE_SCHEMA
                WHERE  tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = @schema
              AND c.TABLE_NAME   = @tableName
            ORDER BY c.ORDINAL_POSITION
            """;

        var parameters = new[]
        {
            new SqlParameter("@schema",    schema),
            new SqlParameter("@tableName", tableName)
        };

        await using var conn = ResolveConnection(serverName, connectionString, database);
        await conn.OpenAsync(cancellationToken);
        var result = await ExecuteReaderAsync(conn, sql, parameters, cancellationToken: cancellationToken);

        if (result == "[]")
            return $"Table '{schema}.{tableName}' was not found in the target database.";

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sql_get_server_info")]
    [Description("Returns SQL Server version, instance name, current database, and edition.")]
    public async Task<string> SqlGetServerInfoAsync(
        [Description("Name of a configured server.")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                @@SERVERNAME                      AS [InstanceName],
                DB_NAME()                         AS [CurrentDatabase],
                SERVERPROPERTY('Edition')         AS [Edition],
                SERVERPROPERTY('ProductVersion')  AS [Version],
                SERVERPROPERTY('ProductLevel')    AS [ServicePack],
                SERVERPROPERTY('EngineEdition')   AS [EngineEdition]
            """;

        await using var conn = ResolveConnection(serverName, connectionString, database: null);
        await conn.OpenAsync(cancellationToken);
        return await ExecuteReaderAsync(conn, sql, [], cancellationToken: cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Query Execution
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "sql_execute_query")]
    [Description(
        "Executes a read-only SQL SELECT query and returns the results as JSON. " +
        "Destructive statements (DELETE / TRUNCATE / DROP / UPDATE) are always blocked here — " +
        "use sql_execute_statement for data modification if it has been enabled. " +
        "Results are capped at 'maxRows' rows (default 500).")]
    public async Task<string> SqlExecuteQueryAsync(
        [Description("The SQL SELECT statement to execute.")]
        string query,
        [Description("Target database name. Overrides the connection's default database.")]
        string? database = null,
        [Description("Name of a configured server.")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        [Description("Maximum number of rows to return (default 500, max 5000).")]
        int maxRows = 500,
        CancellationToken cancellationToken = default)
    {
        // SELECT tool: always block destructive, regardless of config.
        if (SqlSafetyGuard.IsDestructive(query))
        {
            var kw = SqlSafetyGuard.GetBlockedKeyword(query);
            return $"[BLOCKED] The query contains a '{kw}' statement. " +
                   "sql_execute_query only allows SELECT statements. " +
                   "Use sql_execute_statement for data modifications (if enabled).";
        }

        maxRows = Math.Clamp(maxRows, 1, 5000);

        await using var conn = ResolveConnection(serverName, connectionString, database);
        await conn.OpenAsync(cancellationToken);
        return await ExecuteReaderAsync(conn, query, [], maxRows, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sql_execute_statement")]
    [Description(
        "Executes a SQL statement (INSERT, UPDATE, DELETE, TRUNCATE, DROP, or DDL). " +
        "By default, UPDATE / DELETE / TRUNCATE / DROP are DISABLED and will be rejected. " +
        "They can be enabled globally via 'MSSql:AllowDestructiveOperations' in appsettings.json " +
        "or per-server via 'MSSql:Servers[n]:AllowDestructiveOperations'. " +
        "INSERT statements are always permitted. " +
        "Returns the number of rows affected.")]
    public async Task<string> SqlExecuteStatementAsync(
        [Description("The SQL statement to execute.")]
        string statement,
        [Description("Target database name. Overrides the connection's default database.")]
        string? database = null,
        [Description("Name of a configured server (determines the destructive-ops policy).")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        CancellationToken cancellationToken = default)
    {
        // Check safety guard.
        if (SqlSafetyGuard.TryGetBlockedKeyword(statement, out var kw))
        {
            var allowed = factory.IsDestructiveAllowed(serverName);
            if (!allowed)
            {
                return $"[BLOCKED] The statement contains a '{kw}' operation, " +
                       "which is disabled by the current configuration. " +
                       "To enable it, set 'AllowDestructiveOperations: true' in appsettings.json " +
                       "under MSSql (globally) or under MSSql:Servers (per-server).";
            }

            logger.LogWarning(
                "Executing destructive statement ({Keyword}) on server '{Server}', database '{Database}'.",
                kw, serverName ?? "(ad-hoc)", database ?? "(default)");
        }

        await using var conn = ResolveConnection(serverName, connectionString, database);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText    = statement;
        cmd.CommandType    = CommandType.Text;
        cmd.CommandTimeout = 120;

        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return $"Statement executed successfully. Rows affected: {affected}.";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Stored Procedures
    // ═════════════════════════════════════════════════════════════════════════

    [McpServerTool(Name = "sql_list_stored_procedures")]
    [Description("Lists all stored procedures in a database, optionally filtered by schema.")]
    public async Task<string> SqlListStoredProceduresAsync(
        [Description("Target database name. Overrides the connection's default database.")]
        string? database = null,
        [Description("Name of a configured server.")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        [Description("Filter by schema name (default: all schemas).")]
        string? schema = null,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT ROUTINE_SCHEMA AS [Schema],
                   ROUTINE_NAME   AS [ProcedureName],
                   CREATED        AS [CreatedAt],
                   LAST_ALTERED   AS [ModifiedAt]
            FROM   INFORMATION_SCHEMA.ROUTINES
            WHERE  ROUTINE_TYPE = 'PROCEDURE'
            """ +
            (schema is not null ? " AND ROUTINE_SCHEMA = @schema" : string.Empty) +
            " ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

        var parameters = schema is not null
            ? new[] { new SqlParameter("@schema", schema) }
            : [];

        await using var conn = ResolveConnection(serverName, connectionString, database);
        await conn.OpenAsync(cancellationToken);
        return await ExecuteReaderAsync(conn, sql, parameters, cancellationToken: cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "sql_execute_stored_procedure")]
    [Description(
        "Executes a stored procedure and returns the first result set as JSON. " +
        "Supply parameters as a JSON object, e.g. {\"@Id\": 42, \"@Name\": \"Alice\"}.")]
    public async Task<string> SqlExecuteStoredProcedureAsync(
        [Description("Stored procedure name (optionally schema-qualified, e.g. dbo.MyProc).")]
        string procedureName,
        [Description("Parameters as a JSON object: {\"@ParamName\": value, ...}. Pass {} for no parameters.")]
        string parametersJson = "{}",
        [Description("Target database name. Overrides the connection's default database.")]
        string? database = null,
        [Description("Name of a configured server.")]
        string? serverName = null,
        [Description("Ad-hoc connection string (used when serverName is not provided).")]
        string? connectionString = null,
        CancellationToken cancellationToken = default)
    {
        SqlParameter[] parameters;
        try
        {
            parameters = ParseJsonParameters(parametersJson);
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to parse parametersJson: {ex.Message}. " +
                   "Expected format: {{\"@ParamName\": value}}";
        }

        await using var conn = ResolveConnection(serverName, connectionString, database);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText    = procedureName;
        cmd.CommandType    = CommandType.StoredProcedure;
        cmd.CommandTimeout = 120;
        cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await SerializeReaderAsync(reader, maxRows: 500, cancellationToken);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves a <see cref="SqlConnection"/> from either a named server or a raw
    /// connection string.  At least one must be provided.
    /// </summary>
    private SqlConnection ResolveConnection(
        string? serverName, string? connectionString, string? database)
    {
        if (!string.IsNullOrWhiteSpace(serverName))
            return factory.CreateFromServerName(serverName, database);

        if (!string.IsNullOrWhiteSpace(connectionString))
            return factory.CreateFromConnectionString(connectionString, database);

        throw new InvalidOperationException(
            "You must supply either a 'serverName' (from sql_list_configured_servers) " +
            "or an ad-hoc 'connectionString'.");
    }

    private static async Task<string> ExecuteReaderAsync(
        SqlConnection conn,
        string sql,
        SqlParameter[] parameters,
        int maxRows = 500,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText    = sql;
        cmd.CommandType    = CommandType.Text;
        cmd.CommandTimeout = 120;
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await SerializeReaderAsync(reader, maxRows, cancellationToken);
    }

    private static async Task<string> SerializeReaderAsync(
        SqlDataReader reader,
        int maxRows,
        CancellationToken cancellationToken)
    {
        var rows    = new List<Dictionary<string, object?>>();
        var columns = Enumerable.Range(0, reader.FieldCount)
                                .Select(reader.GetName)
                                .ToArray();
        var count = 0;
        var truncated = false;

        while (await reader.ReadAsync(cancellationToken))
        {
            if (count >= maxRows)
            {
                truncated = true;
                break;
            }

            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);

            rows.Add(row);
            count++;
        }

        var payload = new
        {
            RowCount  = rows.Count,
            Truncated = truncated,
            Rows      = rows
        };

        return JsonSerializer.Serialize(payload, JsonOpts);
    }

    private static SqlParameter[] ParseJsonParameters(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            return [];

        using var doc = JsonDocument.Parse(json);
        var list = new List<SqlParameter>();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var name  = prop.Name.StartsWith('@') ? prop.Name : "@" + prop.Name;
            var value = prop.Value.ValueKind switch
            {
                JsonValueKind.Null    => (object)DBNull.Value,
                JsonValueKind.True    => true,
                JsonValueKind.False   => false,
                JsonValueKind.Number  => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                JsonValueKind.String  => prop.Value.GetString()!,
                _                     => prop.Value.GetRawText()
            };

            list.Add(new SqlParameter(name, value));
        }

        return [.. list];
    }
}
