using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Logging.Configuration;

namespace VenEl.MCPAssistant.Logging.Tools;

/// <summary>MCP tool for reading server logs.</summary>
[McpServerToolType]
public class GetServerLogsTool
{
    private readonly FileLoggerOptions _options;

    public GetServerLogsTool(IOptions<FileLoggerOptions> options)
    {
        _options = options.Value;
    }

    [McpServerTool(Name = "get_server_logs")]
    [Description("Retrieves the most recent log lines from the server's active log file. Useful for self-diagnosing errors or checking execution flow.")]
    public async Task<string> GetServerLogsAsync(
        [Description("Number of lines to read from the end of the file. Default is 50, Max is 500.")] int lines = 50,
        CancellationToken cancellationToken = default)
    {
        lines = Math.Clamp(lines, 1, 500);

        if (!Directory.Exists(_options.LogDirectory))
        {
            return "Log directory does not exist.";
        }

        var latestFile = new DirectoryInfo(_options.LogDirectory)
            .GetFiles($"{_options.LogFileNamePrefix}-*.log")
            .OrderByDescending(f => f.CreationTime)
            .FirstOrDefault();

        if (latestFile == null)
        {
            return "No log files found.";
        }

        try
        {
            using var stream = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            
            var allLines = (await reader.ReadToEndAsync(cancellationToken)).Split(Environment.NewLine);
            var resultLines = allLines.Skip(Math.Max(0, allLines.Length - lines)).ToArray();
            
            return string.Join(Environment.NewLine, resultLines);
        }
        catch (Exception ex)
        {
            return $"Failed to read logs: {ex.Message}";
        }
    }
}
