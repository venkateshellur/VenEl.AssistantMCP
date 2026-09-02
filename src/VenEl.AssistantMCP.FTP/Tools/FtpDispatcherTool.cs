using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Dispatcher;

namespace VenEl.AssistantMCP.FTP.Tools;

[McpServerToolType]
public class FtpDispatcherTool : DispatcherToolBase<FtpCommandArgs>
{
    public FtpDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "FTP")
    {
    }

    protected override string? GetRequestedAction(FtpCommandArgs args) => args.Action;

    [McpServerTool(Name = "mcp_ftp_manager")]
    [Description("Manage files and directories over FTP, FTPS, and SFTP. Actions: list, read, download, move, copy. Requires host, username, password, sourcePath (destinationPath for move/copy/download).")]
    public Task<string> ExecuteAsync(FtpCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
