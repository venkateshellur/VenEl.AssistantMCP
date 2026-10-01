using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class ReadEmailActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<ReadEmailActionHandler> _logger;

    public ReadEmailActionHandler(ILogger<ReadEmailActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "read_email";

    public string? Validate(EmailCommandArgs args) => null;

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Reading emails via Outlook is only supported on Windows.");
        }
        return ReadViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> ReadViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic ns = outlookApp.GetNamespace("MAPI");
            dynamic inbox = ns.GetDefaultFolder(6); // olFolderInbox = 6
            
            dynamic items = inbox.Items;
            items.Sort("[ReceivedTime]", true); // descending

            int count = 0;
            int maxCount = args.MaxCount > 0 ? args.MaxCount : 10;
            
            var sb = new StringBuilder();
            sb.AppendLine($"--- Latest {maxCount} Emails ---");
            
            foreach (dynamic item in items)
            {
                if (count >= maxCount) break;
                
                try
                {
                    if (item.Class == 43) // olMail
                    {
                        sb.AppendLine($"EntryID: {item.EntryID}");
                        sb.AppendLine($"Subject: {item.Subject}");
                        sb.AppendLine($"From: {item.SenderName}");
                        sb.AppendLine($"Received: {item.ReceivedTime}");
                        
                        string bodyStr = item.Body?.ToString() ?? "";
                        if (bodyStr.Length > 100) bodyStr = bodyStr.Substring(0, 100) + "...";
                        bodyStr = bodyStr.Replace("\n", " ").Replace("\r", "");
                        
                        sb.AppendLine($"Body Snippet: {bodyStr}");
                        sb.AppendLine(new string('-', 40));
                        count++;
                    }
                }
                catch { /* Ignore items that fail to read properties */ }
            }
            
            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read emails via Outlook.");
            return Task.FromResult($"Failed to read emails: {ex.Message}");
        }
    }
}
