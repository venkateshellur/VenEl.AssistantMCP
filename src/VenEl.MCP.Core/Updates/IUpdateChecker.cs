using System.Threading;
using System.Threading.Tasks;

namespace VenEl.MCP.Core.Updates;

public interface IUpdateChecker
{
    Task<string?> GetUpdateNotificationAsync(CancellationToken ct);
}
