using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.FileManager.Tools;

[McpServerToolType]
public class FileManagerDispatcherTool : DispatcherToolBase<FileManagerCommandArgs>
{
    public FileManagerDispatcherTool(IServiceProvider serviceProvider) 
        : base(serviceProvider, "FileManager")
    {
    }

    protected override string? GetRequestedAction(FileManagerCommandArgs args) => args.Action;

    [McpServerTool(Name = "file_manager")]
    [Description("Manage local files. Actions: list_dir, read_file, write_file, delete_file, move_file, open_ui")]
    public Task<string> ExecuteAsync(FileManagerCommandArgs args, CancellationToken ct)
    {
        return DispatchAsync(args, ct);
    }
}
