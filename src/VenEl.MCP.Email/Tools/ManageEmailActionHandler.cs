using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class ManageEmailActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<ManageEmailActionHandler> _logger;

    public ManageEmailActionHandler(ILogger<ManageEmailActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "manage_email";

    public string? Validate(EmailCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.EntryId)) return "EntryId is required to manage an email.";
        if (string.IsNullOrEmpty(args.ManageAction)) return "ManageAction is required (e.g. read, unread, delete, move).";
        return null;
    }

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Managing emails via Outlook is only supported on Windows.");
        }
        return ManageViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> ManageViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic ns = outlookApp.GetNamespace("MAPI");
            
            // Get item by EntryID
            dynamic mail = ns.GetItemFromID(args.EntryId);
            if (mail == null)
            {
                return Task.FromResult("Could not find the email item.");
            }

            string action = args.ManageAction?.ToLowerInvariant() ?? "";
            switch (action)
            {
                case "read":
                    mail.UnRead = false;
                    mail.Save();
                    return Task.FromResult($"Email marked as read.");
                case "unread":
                    mail.UnRead = true;
                    mail.Save();
                    return Task.FromResult($"Email marked as unread.");
                case "delete":
                    mail.Delete();
                    return Task.FromResult($"Email deleted.");
                default:
                    return Task.FromResult($"Unknown ManageAction: {action}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to manage email via Outlook.");
            return Task.FromResult($"Failed to manage email: {ex.Message}");
        }
    }
}
