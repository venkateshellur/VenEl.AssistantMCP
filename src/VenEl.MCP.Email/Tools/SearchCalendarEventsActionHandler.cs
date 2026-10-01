using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class SearchCalendarEventsActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<SearchCalendarEventsActionHandler> _logger;

    public SearchCalendarEventsActionHandler(ILogger<SearchCalendarEventsActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "search_calendar_events";

    public string? Validate(EmailCommandArgs args) => null;

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Searching calendar events via Outlook is only supported on Windows.");
        }
        return SearchCalendarEventsViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> SearchCalendarEventsViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
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
            
            var conditions = new List<string>();
            
            // To use IncludeRecurrences, we need a bounding start date. Let's use 1 year ago.
            string oneYearAgo = DateTime.Today.AddYears(-1).ToString("MM/dd/yyyy hh:mm tt");
            items = items.Restrict($"[Start] >= '{oneYearAgo}'");
            

            if (!string.IsNullOrEmpty(args.SearchSubject))
                conditions.Add($"\"urn:schemas:httpmail:subject\" ci_phrasematch '{args.SearchSubject.Replace("'", "''")}'");
            
            if (!string.IsNullOrEmpty(args.SearchLocation))
                conditions.Add($"\"urn:schemas:calendar:location\" ci_phrasematch '{args.SearchLocation.Replace("'", "''")}'");
            
            if (!string.IsNullOrEmpty(args.SearchBody))
                conditions.Add($"\"urn:schemas:httpmail:textdescription\" ci_phrasematch '{args.SearchBody.Replace("'", "''")}'");

            if (conditions.Count > 0)
            {
                string filter = $"@SQL=" + string.Join(" AND ", conditions);
                items = items.Restrict(filter);
            }
            
            int count = 0;
            int maxCount = args.MaxCount > 0 ? args.MaxCount : 10;
            
            var sb = new StringBuilder();
            sb.AppendLine($"--- Top {maxCount} Calendar Search Results ---");
            
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
                        
                        string bodyStr = item.Body?.ToString() ?? "";
                        if (bodyStr.Length > 100) bodyStr = bodyStr.Substring(0, 100) + "...";
                        bodyStr = bodyStr.Replace("\n", " ").Replace("\r", "");
                        
                        sb.AppendLine($"Body Snippet: {bodyStr}");
                        sb.AppendLine(new string('-', 40));
                        count++;
                    }
                }
                catch { }
            }
            
            if (count == 0)
            {
                sb.AppendLine("No calendar events found matching the criteria.");
            }
            
            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search calendar events via Outlook.");
            return Task.FromResult($"Failed to search calendar events: {ex.Message}");
        }
    }
}
