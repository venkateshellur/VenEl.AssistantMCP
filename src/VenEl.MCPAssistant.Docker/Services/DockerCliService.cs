using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VenEl.MCPAssistant.Docker.Services;

public interface IDockerCliService
{
    Task<string> ExecuteCommandAsync(string arguments, CancellationToken ct);
}

public class DockerCliService(ILogger<DockerCliService> logger) : IDockerCliService
{
    public async Task<string> ExecuteCommandAsync(string arguments, CancellationToken ct)
    {
        logger.LogDebug("Executing Docker command: docker {Arguments}", arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start docker process.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                var errorMsg = !string.IsNullOrWhiteSpace(error) ? error : output;
                logger.LogError("Docker command failed with exit code {ExitCode}: {Error}", process.ExitCode, errorMsg);
                return $"[ERROR] Docker command failed (Exit {process.ExitCode}): {errorMsg.Trim()}";
            }

            // Docker sometimes writes non-errors to stderr (like download progress).
            // But if exit code is 0, we generally favor stdout.
            if (!string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                return error.Trim();
            }

            return "Command executed successfully with no output.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while executing docker command.");
            return $"[ERROR] {ex.Message}";
        }
    }
}
