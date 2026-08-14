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
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _activeAlerts = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AlertsManager(ILogger<AlertsManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAlertsAsync(IEnumerable<string> alerts, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _activeAlerts.Clear();
            _activeAlerts.AddRange(alerts);
            _logger.LogInformation($"Published {_activeAlerts.Count} new proactive alerts.");
            
            try
            {
                var mcpServer = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ModelContextProtocol.Server.McpServer>(_serviceProvider);
                if (mcpServer != null)
                {
                    await mcpServer.SendNotificationAsync("notifications/resources/updated", new { uri = "venel://system/latest-alerts" }, cancellationToken: ct);
                    _logger.LogInformation("Sent notifications/resources/updated to connected MCP clients.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send proactive notification to MCP Server.");
            }
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
