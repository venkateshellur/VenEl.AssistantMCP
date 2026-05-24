using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Logging.Tools;

[McpServerToolType]
public class LoggingDispatcherTool : DispatcherToolBase<LoggingCommandArgs>
{
    public LoggingDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Logging")
    {
    }

    protected override string? GetRequestedAction(LoggingCommandArgs args) => args.Action;

    [McpServerTool(Name = "logging_commands")]
    [Description("Server diagnostic and logging commands.")]
    public Task<string> ExecuteAsync(LoggingCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
