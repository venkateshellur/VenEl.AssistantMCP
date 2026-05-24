using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.Azure.Configuration;

using VenEl.MCPAssistant.Core.Security;

namespace VenEl.MCPAssistant.Azure.Services.Auth;

/// <summary>
/// Provides Basic Auth headers using Azure Personal Access Tokens (PATs).
/// </summary>
public sealed class PatAuthProvider(
    IOptions<AzureOptions> options,
    AzureSessionCredentials session,
    SecretManager secretManager) : IAzureAuthProvider
{
    private readonly AzureOptions _opts = options.Value;

    public async Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(CancellationToken cancellationToken)
    {
        // Session takes precedence over config
        var rawPat = session.HasPatToken ? session.PatToken : _opts.Pat.Token;
        
        var pat = await secretManager.ResolveSecretAsync(rawPat, cancellationToken);

        if (string.IsNullOrWhiteSpace(pat))
        {
            return null;
        }

        // Azure DevOps PATs use Basic Auth with an empty username
        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }
}
