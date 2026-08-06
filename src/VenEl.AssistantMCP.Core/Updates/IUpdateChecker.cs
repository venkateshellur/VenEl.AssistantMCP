using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.Core.Updates;

public interface IUpdateChecker
{
    Task<string?> GetUpdateNotificationAsync(CancellationToken ct);
}
