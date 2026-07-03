using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.Email.Tools;

[McpServerToolType]
public class EmailDispatcherTool : DispatcherToolBase<EmailCommandArgs>
{
    public EmailDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "Email")
    {
    }

    protected override string? GetRequestedAction(EmailCommandArgs args) => args.Action;

    [McpServerTool(Name = "email_commands")]
    [Description("Send emails and perform email operations. Actions: send_email")]
    public Task<string> ExecuteAsync(EmailCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
