using System.Threading;
using System.Threading.Tasks;

namespace VenEl.AssistantMCP.Core.Security;

public interface ISecretResolver
{
    Task<(bool Handled, string? Value)> TryResolveAsync(string identifier, CancellationToken cancellationToken = default);
}
