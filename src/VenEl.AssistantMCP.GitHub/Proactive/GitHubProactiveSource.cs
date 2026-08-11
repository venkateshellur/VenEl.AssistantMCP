using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Proactive;

namespace VenEl.AssistantMCP.GitHub.Proactive;

public class GitHubProactiveSource : IProactiveSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubProactiveSource> _logger;
    private string? _lastSeenId;

    public GitHubProactiveSource(HttpClient httpClient, ILogger<GitHubProactiveSource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> CheckForNewAlertsAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("/notifications?participating=true", ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var notifications = await response.Content.ReadFromJsonAsync<GitHubNotification[]>(cancellationToken: ct);
            if (notifications == null || notifications.Length == 0)
            {
                return null;
            }

            var latest = notifications[0];
            if (latest.Id == _lastSeenId)
            {
                return null;
            }

            _lastSeenId = latest.Id;
            return $"[GitHub Alert] You have a new unread notification on {latest.Repository?.FullName}: '{latest.Subject?.Title}' ({latest.Reason})";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to poll GitHub notifications.");
            return null;
        }
    }
    
    private class GitHubNotification
    {
        public string? Id { get; set; }
        public GitHubSubject? Subject { get; set; }
        public GitHubRepository? Repository { get; set; }
        public string? Reason { get; set; }
    }
    
    private class GitHubSubject
    {
        public string? Title { get; set; }
    }
    
    private class GitHubRepository
    {
        public string? FullName { get; set; }
    }
}
