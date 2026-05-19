using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using VenEl.MCPAssistant.Logging.Configuration;

using VenEl.MCPAssistant.Core.Dispatcher;

namespace VenEl.MCPAssistant.Logging.Tools;

/// <summary>Handler for reading server logs.</summary>
public class GetServerLogsActionHandler : IActionHandler<LoggingCommandArgs>
{
    private readonly FileLoggerOptions _options;

    public GetServerLogsActionHandler(IOptions<FileLoggerOptions> options)
    {
        _options = options.Value;
    }

    public string ActionName => "get_server_logs";

    public string? Validate(LoggingCommandArgs args) => null; // all parameters are optional

    public async Task<string> HandleAsync(LoggingCommandArgs args, CancellationToken ct)
    {
        int lines = Math.Clamp(args.Lines ?? 50, 1, 500);

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
            
            var allLines = (await reader.ReadToEndAsync(ct)).Split(Environment.NewLine);
            var resultLines = allLines.Skip(Math.Max(0, allLines.Length - lines)).ToArray();
            
            return string.Join(Environment.NewLine, resultLines);
        }
        catch (Exception ex)
        {
            return $"Failed to read logs: {ex.Message}";
        }
    }
}
