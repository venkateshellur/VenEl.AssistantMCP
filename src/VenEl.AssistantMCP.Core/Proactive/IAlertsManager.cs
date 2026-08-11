using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.Core.Proactive;

/// <summary>
/// Manages proactive alerts and notifies the MCP client.
/// </summary>
public interface IAlertsManager
{
    /// <summary>
    /// Publishes new alerts and triggers a resource update notification.
    /// </summary>
    Task PublishAlertsAsync(IEnumerable<string> alerts, CancellationToken ct);
    
    /// <summary>
    /// Retrieves all currently active alerts.
    /// </summary>
    Task<string> GetActiveAlertsAsync(CancellationToken ct);
}
