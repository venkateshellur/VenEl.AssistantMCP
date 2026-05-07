using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using VenEl.MCPAssistant.Azure.Configuration;

namespace VenEl.MCPAssistant.Azure.Services.Auth;

/// <summary>
/// Provides Basic Auth headers using Azure Personal Access Tokens (PATs).
/// </summary>
public sealed class PatAuthProvider(
    IOptions<AzureOptions> options,
    AzureSessionCredentials session) : IAzureAuthProvider
{
    private readonly AzureOptions _opts = options.Value;

    public Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(CancellationToken cancellationToken)
    {
        // Session takes precedence over config
        var pat = session.HasPatToken ? session.PatToken : _opts.Pat.Token;

        if (string.IsNullOrWhiteSpace(pat))
        {
            return Task.FromResult<AuthenticationHeaderValue?>(null);
        }

        // Azure DevOps PATs use Basic Auth with an empty username
        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        return Task.FromResult<AuthenticationHeaderValue?>(new AuthenticationHeaderValue("Basic", encoded));
    }
}
