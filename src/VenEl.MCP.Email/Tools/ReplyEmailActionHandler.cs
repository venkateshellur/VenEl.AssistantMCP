using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class ReplyEmailActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<ReplyEmailActionHandler> _logger;

    public ReplyEmailActionHandler(ILogger<ReplyEmailActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "reply_email";

    public string? Validate(EmailCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.EntryId)) return "EntryId is required to reply to an email.";
        if (string.IsNullOrEmpty(args.Body)) return "Body is required to send a reply.";
        return null;
    }

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Replying to emails via Outlook is only supported on Windows.");
        }
        return ReplyViaOutlookAsync(args, ct);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> ReplyViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic ns = outlookApp.GetNamespace("MAPI");
            
            // Get item by EntryID
            dynamic originalMail = ns.GetItemFromID(args.EntryId);
            if (originalMail == null || originalMail.Class != 43) // 43 = olMail
            {
                return Task.FromResult("Could not find the original email or it is not a mail item.");
            }

            dynamic replyMail = originalMail.ReplyAll();
            
            if (args.IsHtml)
                replyMail.HTMLBody = args.Body + replyMail.HTMLBody;
            else
                replyMail.Body = args.Body + "\n\n" + replyMail.Body;
            
            replyMail.Send();

            return Task.FromResult($"Successfully replied to email: {originalMail.Subject}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reply to email via Outlook.");
            return Task.FromResult($"Failed to reply to email: {ex.Message}");
        }
    }
}
