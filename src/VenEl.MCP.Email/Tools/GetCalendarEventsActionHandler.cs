using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class GetCalendarEventsActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<GetCalendarEventsActionHandler> _logger;

    public GetCalendarEventsActionHandler(ILogger<GetCalendarEventsActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "get_calendar_events";

    public string? Validate(EmailCommandArgs args) => null;

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Reading calendar events via Outlook is only supported on Windows.");
        }
        return GetCalendarEventsViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> GetCalendarEventsViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic ns = outlookApp.GetNamespace("MAPI");
            dynamic calendarFolder = ns.GetDefaultFolder(9); // olFolderCalendar = 9
            
            dynamic items = calendarFolder.Items;
            items.Sort("[Start]", false);
            items.IncludeRecurrences = true;
            
            // Restrict to future events (from today)
            string today = DateTime.Today.ToString("MM/dd/yyyy hh:mm tt");
            items = items.Restrict($"[Start] >= '{today}'");

            int count = 0;
            int maxCount = args.MaxCount > 0 ? args.MaxCount : 10;
            
            var sb = new StringBuilder();
            sb.AppendLine($"--- Next {maxCount} Calendar Events ---");
            
            foreach (dynamic item in items)
            {
                if (count >= maxCount) break;
                
                try
                {
                    if (item.Class == 26) // olAppointment
                    {
                        sb.AppendLine($"Subject: {item.Subject}");
                        sb.AppendLine($"Start: {item.Start}");
                        sb.AppendLine($"End: {item.End}");
                        sb.AppendLine($"Location: {item.Location}");
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
            _logger.LogError(ex, "Failed to read calendar events via Outlook.");
            return Task.FromResult($"Failed to read calendar events: {ex.Message}");
        }
    }
}
