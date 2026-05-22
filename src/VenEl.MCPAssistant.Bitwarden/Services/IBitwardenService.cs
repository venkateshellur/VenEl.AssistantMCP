using System.Threading;
using System.Threading.Tasks;

namespace VenEl.MCPAssistant.Bitwarden.Services;

public interface IBitwardenService
{
    Task<string> GetSecretAsync(string identifier, CancellationToken cancellationToken);
}
