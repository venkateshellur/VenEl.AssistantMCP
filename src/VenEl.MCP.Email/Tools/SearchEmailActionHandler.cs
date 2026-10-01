using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class SearchEmailActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<SearchEmailActionHandler> _logger;

    public SearchEmailActionHandler(ILogger<SearchEmailActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "search_email";

    public string? Validate(EmailCommandArgs args) => null;

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Searching emails via Outlook is only supported on Windows.");
        }
        return SearchViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> SearchViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic ns = outlookApp.GetNamespace("MAPI");
            dynamic inbox = ns.GetDefaultFolder(6); // olFolderInbox = 6
            
            dynamic items = inbox.Items;
            
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(args.SearchSubject))
                conditions.Add($"\"urn:schemas:httpmail:subject\" ci_phrasematch '{args.SearchSubject.Replace("'", "''")}'");
            
            if (!string.IsNullOrEmpty(args.SearchSender))
                conditions.Add($"\"urn:schemas:httpmail:sendername\" ci_phrasematch '{args.SearchSender.Replace("'", "''")}'");
            
            if (!string.IsNullOrEmpty(args.SearchBody))
                conditions.Add($"\"urn:schemas:httpmail:textdescription\" ci_phrasematch '{args.SearchBody.Replace("'", "''")}'");

            if (conditions.Count > 0)
            {
                string filter = $"@SQL=" + string.Join(" AND ", conditions);
                items = items.Restrict(filter);
            }
            
            items.Sort("[ReceivedTime]", true); // descending

            int count = 0;
            int maxCount = args.MaxCount > 0 ? args.MaxCount : 10;
            
            var sb = new StringBuilder();
            sb.AppendLine($"--- Top {maxCount} Search Results ---");
            
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
            
            if (count == 0)
            {
                sb.AppendLine("No emails found matching the criteria.");
            }
            
            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search emails via Outlook.");
            return Task.FromResult($"Failed to search emails: {ex.Message}");
        }
    }
}
