using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.Core.Proactive;

/// <summary>
/// Implemented by integrations (like GitHub, Jira) to poll for recent events.
/// </summary>
public interface IProactiveSource
{
    /// <summary>
    /// Checks the source for new events and returns a formatted alert string.
    /// If there are no new events, it should return null.
    /// </summary>
    Task<string?> CheckForNewAlertsAsync(CancellationToken ct);
}
