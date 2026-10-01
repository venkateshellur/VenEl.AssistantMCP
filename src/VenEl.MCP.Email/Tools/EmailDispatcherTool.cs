using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

[McpServerToolType]
public class EmailDispatcherTool : DispatcherToolBase<EmailCommandArgs>
{
    public EmailDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Email")
    {
    }

    protected override string? GetRequestedAction(EmailCommandArgs args) => args.Action;

    [McpServerTool(Name = "email_commands")]
    [Description("Send emails and perform email operations. Actions: send_email, read_email, search_email, get_tasks, get_calendar_events, search_calendar_events, reply_email, manage_email, create_calendar_event, create_task")]
    public Task<string> ExecuteAsync(EmailCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
