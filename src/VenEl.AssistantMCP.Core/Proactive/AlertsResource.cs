using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using VenEl.AssistantMCP.Core.Proactive;

namespace VenEl.AssistantMCP.Core.Proactive;

[McpServerResourceType]
public class AlertsResource
{
    private readonly IAlertsManager _alertsManager;

    public AlertsResource(IAlertsManager alertsManager)
    {
        _alertsManager = alertsManager;
    }

    [McpServerResource(UriTemplate = "venel://system/latest-alerts", Name = "latest-alerts", Title = "Latest Proactive Alerts", MimeType = "text/plain")]
    public async Task<string> GetLatestAlertsAsync(CancellationToken ct)
    {
        return await _alertsManager.GetActiveAlertsAsync(ct);
    }
}
