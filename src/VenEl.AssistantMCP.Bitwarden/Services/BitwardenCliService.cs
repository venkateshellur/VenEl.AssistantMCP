using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VenEl.AssistantMCP.Bitwarden.Services;

public class BitwardenCliService : IBitwardenService
{
    private readonly ILogger<BitwardenCliService> _logger;

    public BitwardenCliService(ILogger<BitwardenCliService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string identifier, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to retrieve secret via local Bitwarden CLI (bw get password)...");
        
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "bw",
                Arguments = $"get password \"{identifier}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start 'bw' process. Is Bitwarden CLI installed?");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("Bitwarden CLI error: {Error}", error);
                throw new InvalidOperationException($"Bitwarden CLI exited with code {process.ExitCode}. Ensure your vault is unlocked. Error: {error}");
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute 'bw get password'.");
            throw;
        }
    }
}
