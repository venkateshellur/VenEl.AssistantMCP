using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.MCP.Core.Dispatcher;

namespace VenEl.MCP.Email.Tools;

public class CreateCalendarEventActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly ILogger<CreateCalendarEventActionHandler> _logger;

    public CreateCalendarEventActionHandler(ILogger<CreateCalendarEventActionHandler> logger)
    {
        _logger = logger;
    }

    public string ActionName => "create_calendar_event";

    public string? Validate(EmailCommandArgs args)
    {
        if (string.IsNullOrEmpty(args.Subject)) return "Subject is required to create an event.";
        if (args.EventStart == null) return "EventStart is required.";
        if (args.EventEnd == null) return "EventEnd is required.";
        return null;
    }

    public Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult("Creating calendar events via Outlook is only supported on Windows.");
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
            dynamic appointment = outlookApp.CreateItem(1); // 1 = olAppointmentItem
            
            appointment.Subject = args.Subject;
            appointment.Start = args.EventStart.Value;
            appointment.End = args.EventEnd.Value;
            
            if (!string.IsNullOrEmpty(args.EventLocation))
                appointment.Location = args.EventLocation;
            
            if (!string.IsNullOrEmpty(args.Body))
                appointment.Body = args.Body;

            // Save creates it in the default calendar without sending invites (unless Recipient is added and MeetingStatus changed)
            appointment.Save();

            return Task.FromResult($"Successfully created calendar event: {args.Subject}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event via Outlook.");
            return Task.FromResult($"Failed to create calendar event: {ex.Message}");
        }
    }
}
