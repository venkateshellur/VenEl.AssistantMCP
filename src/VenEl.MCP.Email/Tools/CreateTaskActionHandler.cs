using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class CreateTaskActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<CreateTaskActionHandler> _logger;

    public CreateTaskActionHandler(ILogger<CreateTaskActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "create_task";

    public string? Validate(EmailCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.Subject)) return "Subject is required to create a task.";
        return null;
    }

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Creating tasks via Outlook is only supported on Windows.");
        }
        return CreateViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> CreateViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic task = outlookApp.CreateItem(3); // 3 = olTaskItem
            
            task.Subject = args.Subject;
            
            if (args.EventStart != null)
                task.DueDate = args.EventStart.Value; // Reusing EventStart for DueDate
                
            if (!string.IsNullOrEmpty(args.Body))
                task.Body = args.Body;

            task.Save();

            return Task.FromResult($"Successfully created task: {args.Subject}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task via Outlook.");
            return Task.FromResult($"Failed to create task: {ex.Message}");
        }
    }
}
