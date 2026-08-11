using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VenEl.AssistantMCP.Core.Proactive;

public class AlertsManager : IAlertsManager
{
    private readonly ILogger<AlertsManager> _logger;
    private readonly List<string> _activeAlerts = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AlertsManager(ILogger<AlertsManager> logger)
    {
        _logger = logger;
    }

    public async Task PublishAlertsAsync(IEnumerable<string> alerts, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _activeAlerts.Clear();
            _activeAlerts.AddRange(alerts);
            _logger.LogInformation($"Published {_activeAlerts.Count} new proactive alerts.");
            
            // TODO: Trigger MCP resource updated notification for venel://system/latest-alerts
            // This requires the underlying MCP server context to send a notifications/resources/updated event.
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> GetActiveAlertsAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!_activeAlerts.Any())
            {
                return "No new alerts at this time.";
            }

            return string.Join("\n\n---\n\n", _activeAlerts);
        }
        finally
        {
            _lock.Release();
        }
    }
}
