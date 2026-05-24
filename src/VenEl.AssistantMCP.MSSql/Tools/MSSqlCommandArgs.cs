using System.ComponentModel;

namespace VenEl.AssistantMCP.MSSql.Tools;

public class MSSqlCommandArgs
{
    [Description("The action to perform. Options: sql_list_configured_servers, sql_list_databases, sql_list_tables, sql_describe_table, sql_get_server_info, sql_execute_query, sql_execute_statement, sql_list_stored_procedures, sql_execute_stored_procedure")]
    public string Action { get; set; } = string.Empty;

    // Connection parameters
    [Description("Name of a configured server (from sql_list_configured_servers).")]
    public string? ServerName { get; set; }

    [Description("Ad-hoc connection string (used when serverName is not provided).")]
    public string? ConnectionString { get; set; }

    [Description("Target database name. Overrides the connection's default database.")]
    public string? Database { get; set; }

    // Object parameters
    [Description("Filter by schema name (default: all schemas). Used by sql_list_tables, sql_describe_table, sql_list_stored_procedures.")]
    public string? Schema { get; set; }

    [Description("Table name (without schema prefix). Used by sql_describe_table.")]
    public string? TableName { get; set; }

    [Description("Stored procedure name (optionally schema-qualified, e.g. dbo.MyProc). Used by sql_execute_stored_procedure.")]
    public string? ProcedureName { get; set; }

    // Execution parameters
    [Description("The SQL SELECT statement to execute. Used by sql_execute_query.")]
    public string? Query { get; set; }

    [Description("The SQL statement to execute (INSERT, UPDATE, DELETE, etc). Used by sql_execute_statement.")]
    public string? Statement { get; set; }

    [Description("Maximum number of rows to return (default 500, max 5000). Used by sql_execute_query.")]
    public int? MaxRows { get; set; }

    [Description("Parameters as a JSON object: {\"@ParamName\": value, ...}. Pass {} for no parameters. Used by sql_execute_stored_procedure.")]
    public string? ParametersJson { get; set; }

    [Description("Path to save Excel export to. Used by sql_export_to_excel.")]
    public string? ExcelExportPath { get; set; }
}
