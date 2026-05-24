using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.MSSql.Guards;
using VenEl.AssistantMCP.MSSql.Services;
using ClosedXML.Excel;
using System.IO;

namespace VenEl.AssistantMCP.MSSql.Tools;

public static class MSSqlHelper
{
    public static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static SqlConnection ResolveConnection(
        ISqlConnectionFactory factory, string? serverName, string? connectionString, string? database)
    {
        if (!string.IsNullOrWhiteSpace(serverName))
            return factory.CreateFromServerName(serverName, database);

        if (!string.IsNullOrWhiteSpace(connectionString))
            return factory.CreateFromConnectionString(connectionString, database);

        throw new InvalidOperationException(
            "You must supply either a 'ServerName' (from sql_list_configured_servers) " +
            "or an ad-hoc 'ConnectionString'.");
    }

    public static async Task<string> ExecuteReaderAsync(
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

    public static async Task<string> SerializeReaderAsync(
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

    public static SqlParameter[] ParseJsonParameters(string json)
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

public sealed class SqlListConfiguredServersActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_list_configured_servers";

    public string? Validate(MSSqlCommandArgs args) => null;

    public Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        var servers = factory.GetAllServers();

        if (servers.Count == 0)
            return Task.FromResult("No servers are currently configured in appsettings.json under MSSql:Servers.");

        var result = servers.Select(s => new
        {
            s.Name,
            s.Description,
            s.DefaultDatabase,
            DestructiveOperationsAllowed = factory.IsDestructiveAllowed(s.Name)
        });

        return Task.FromResult(JsonSerializer.Serialize(result, MSSqlHelper.JsonOpts));
    }
}

public sealed class SqlListDatabasesActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_list_databases";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        const string sql = """
            SELECT name, state_desc AS [Status], create_date AS [CreatedAt]
            FROM   sys.databases
            WHERE  name NOT IN ('master','tempdb','model','msdb')
            ORDER  BY name
            """;

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, null);
        await conn.OpenAsync(ct);
        return await MSSqlHelper.ExecuteReaderAsync(conn, sql, [], cancellationToken: ct);
    }
}

public sealed class SqlListTablesActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_list_tables";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        var sql = """
            SELECT TABLE_SCHEMA  AS [Schema],
                   TABLE_NAME    AS [Table],
                   TABLE_TYPE    AS [Type]
            FROM   INFORMATION_SCHEMA.TABLES
            WHERE  TABLE_TYPE = 'BASE TABLE'
            """ +
            (args.Schema is not null ? " AND TABLE_SCHEMA = @schema" : string.Empty) +
            " ORDER BY TABLE_SCHEMA, TABLE_NAME";

        var parameters = args.Schema is not null
            ? new[] { new SqlParameter("@schema", args.Schema) }
            : [];

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);
        return await MSSqlHelper.ExecuteReaderAsync(conn, sql, parameters, cancellationToken: ct);
    }
}

public sealed class SqlDescribeTableActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_describe_table";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        if (string.IsNullOrWhiteSpace(args.TableName))
            return "Missing required parameter 'TableName'.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
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

        string schema = string.IsNullOrWhiteSpace(args.Schema) ? "dbo" : args.Schema;

        var parameters = new[]
        {
            new SqlParameter("@schema", schema),
            new SqlParameter("@tableName", args.TableName)
        };

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);
        var result = await MSSqlHelper.ExecuteReaderAsync(conn, sql, parameters, cancellationToken: ct);

        if (result == "[]")
            return $"Table '{schema}.{args.TableName}' was not found in the target database.";

        return result;
    }
}

public sealed class SqlGetServerInfoActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_get_server_info";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
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

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, null);
        await conn.OpenAsync(ct);
        return await MSSqlHelper.ExecuteReaderAsync(conn, sql, [], cancellationToken: ct);
    }
}

public sealed class SqlExecuteQueryActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_execute_query";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        if (string.IsNullOrWhiteSpace(args.Query))
            return "Missing required parameter 'Query'.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        if (SqlSafetyGuard.IsDestructive(args.Query!))
        {
            var kw = SqlSafetyGuard.GetBlockedKeyword(args.Query!);
            return $"[BLOCKED] The query contains a '{kw}' statement. " +
                   "sql_execute_query only allows SELECT statements. " +
                   "Use sql_execute_statement for data modifications (if enabled).";
        }

        int maxRows = Math.Clamp(args.MaxRows ?? 500, 1, 5000);

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);
        return await MSSqlHelper.ExecuteReaderAsync(conn, args.Query!, [], maxRows, ct);
    }
}

public sealed class SqlExecuteStatementActionHandler(ISqlConnectionFactory factory, ILogger<SqlExecuteStatementActionHandler> logger) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_execute_statement";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        if (string.IsNullOrWhiteSpace(args.Statement))
            return "Missing required parameter 'Statement'.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        if (SqlSafetyGuard.TryGetBlockedKeyword(args.Statement!, out var kw))
        {
            var allowed = factory.IsDestructiveAllowed(args.ServerName);
            if (!allowed)
            {
                return $"[BLOCKED] The statement contains a '{kw}' operation, " +
                       "which is disabled by the current configuration. " +
                       "To enable it, set 'AllowDestructiveOperations: true' in appsettings.json " +
                       "under MSSql (globally) or under MSSql:Servers (per-server).";
            }

            logger.LogWarning(
                "Executing destructive statement ({Keyword}) on server '{Server}', database '{Database}'.",
                kw, args.ServerName ?? "(ad-hoc)", args.Database ?? "(default)");
        }

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText    = args.Statement!;
        cmd.CommandType    = CommandType.Text;
        cmd.CommandTimeout = 120;

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return $"Statement executed successfully. Rows affected: {affected}.";
    }
}

public sealed class SqlListStoredProceduresActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_list_stored_procedures";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        var sql = """
            SELECT ROUTINE_SCHEMA AS [Schema],
                   ROUTINE_NAME   AS [ProcedureName],
                   CREATED        AS [CreatedAt],
                   LAST_ALTERED   AS [ModifiedAt]
            FROM   INFORMATION_SCHEMA.ROUTINES
            WHERE  ROUTINE_TYPE = 'PROCEDURE'
            """ +
            (args.Schema is not null ? " AND ROUTINE_SCHEMA = @schema" : string.Empty) +
            " ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";

        var parameters = args.Schema is not null
            ? new[] { new SqlParameter("@schema", args.Schema) }
            : [];

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);
        return await MSSqlHelper.ExecuteReaderAsync(conn, sql, parameters, cancellationToken: ct);
    }
}

public sealed class SqlExecuteStoredProcedureActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_execute_stored_procedure";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        if (string.IsNullOrWhiteSpace(args.ProcedureName))
            return "Missing required parameter 'ProcedureName'.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        SqlParameter[] parameters;
        try
        {
            parameters = MSSqlHelper.ParseJsonParameters(args.ParametersJson ?? "{}");
        }
        catch (Exception ex)
        {
            return $"[ERROR] Failed to parse ParametersJson: {ex.Message}. " +
                   "Expected format: {{\"@ParamName\": value}}";
        }

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText    = args.ProcedureName!;
        cmd.CommandType    = CommandType.StoredProcedure;
        cmd.CommandTimeout = 120;
        cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await MSSqlHelper.SerializeReaderAsync(reader, maxRows: 500, ct);
    }
}

public sealed class SqlExportToExcelActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_export_to_excel";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        if (string.IsNullOrWhiteSpace(args.Query))
            return "Missing required parameter 'Query'.";
        if (string.IsNullOrWhiteSpace(args.ExcelExportPath))
            return "Missing required parameter 'ExcelExportPath'.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        if (SqlSafetyGuard.IsDestructive(args.Query!))
        {
            return $"[BLOCKED] Destructive queries are not allowed for Excel export.";
        }

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = args.Query!;
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("QueryResults");
        
        // Write headers
        for (int i = 0; i < reader.FieldCount; i++)
        {
            worksheet.Cell(1, i + 1).Value = reader.GetName(i);
        }

        // Write rows
        int rowIdx = 2;
        while (await reader.ReadAsync(ct))
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (!reader.IsDBNull(i))
                {
                    worksheet.Cell(rowIdx, i + 1).Value = XLCellValue.FromObject(reader.GetValue(i));
                }
            }
            rowIdx++;
        }

        workbook.SaveAs(args.ExcelExportPath!);
        return $"Query results successfully exported to {args.ExcelExportPath}";
    }
}

public sealed class SqlGenerateSchemaActionHandler(ISqlConnectionFactory factory) : IActionHandler<MSSqlCommandArgs>
{
    public string ActionName => "sql_generate_schema";

    public string? Validate(MSSqlCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.ServerName) && string.IsNullOrWhiteSpace(args.ConnectionString))
            return "You must provide either ServerName or ConnectionString.";
        return null;
    }

    public async Task<string> HandleAsync(MSSqlCommandArgs args, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                TABLE_SCHEMA + '.' + TABLE_NAME as TableName, 
                COLUMN_NAME as ColumnName, 
                DATA_TYPE as DataType 
            FROM INFORMATION_SCHEMA.COLUMNS
            ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION
        ";

        await using var conn = MSSqlHelper.ResolveConnection(factory, args.ServerName, args.ConnectionString, args.Database);
        await conn.OpenAsync(ct);
        return await MSSqlHelper.ExecuteReaderAsync(conn, sql, [], 5000, ct);
    }
}
