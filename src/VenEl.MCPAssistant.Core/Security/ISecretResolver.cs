using System.Threading;
using System.Threading.Tasks;

namespace VenEl.MCPAssistant.Core.Security;

public interface ISecretResolver
{
    Task<(bool Handled, string? Value)> TryResolveAsync(string identifier, CancellationToken cancellationToken = default);
}
