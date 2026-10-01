using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class GetTasksActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<GetTasksActionHandler> _logger;

    public GetTasksActionHandler(ILogger<GetTasksActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "get_tasks";

    public string? Validate(EmailCommandArgs args) => null;

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Reading tasks via Outlook is only supported on Windows.");
        }
        return GetTasksViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> GetTasksViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic ns = outlookApp.GetNamespace("MAPI");
            dynamic tasksFolder = ns.GetDefaultFolder(13); // olFolderTasks = 13
            
            dynamic items = tasksFolder.Items;
            
            int count = 0;
            int maxCount = args.MaxCount > 0 ? args.MaxCount : 10;
            
            var sb = new StringBuilder();
            sb.AppendLine($"--- Latest {maxCount} Tasks ---");
            
            foreach (dynamic item in items)
            {
                if (count >= maxCount) break;
                
                try
                {
                    if (item.Class == 48) // olTask
                    {
                        sb.AppendLine($"Subject: {item.Subject}");
                        sb.AppendLine($"Due Date: {item.DueDate}");
                        sb.AppendLine($"Status: {item.Status}");
                        sb.AppendLine(new string('-', 40));
                        count++;
                    }
                }
                catch { }
            }
            
            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read tasks via Outlook.");
            return Task.FromResult($"Failed to read tasks: {ex.Message}");
        }
    }
}
