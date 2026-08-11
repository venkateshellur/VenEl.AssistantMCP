using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Atlassian.Services;
using VenEl.AssistantMCP.Core.Proactive;

namespace VenEl.AssistantMCP.Atlassian.Proactive;

public class AtlassianProactiveSource : IProactiveSource
{
    private readonly IAtlassianHttpClient _httpClient;
    private readonly ILogger<AtlassianProactiveSource> _logger;
    private DateTime _lastSeenUpdate = DateTime.UtcNow.AddMinutes(-5); // Startup grace period

    public AtlassianProactiveSource(IAtlassianHttpClient httpClient, ILogger<AtlassianProactiveSource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> CheckForNewAlertsAsync(CancellationToken ct)
    {
        try
        {
            // We search for tickets assigned to the current user that were updated very recently.
            // Jira JQL supports minute resolution using 'm'. E.g. -5m
            var jql = "assignee = currentUser() AND updated >= -5m ORDER BY updated DESC";
            
            var payload = new
            {
                jql = jql,
                maxResults = 5,
                fields = new[] { "summary", "updated", "status" }
            };

            var responseJson = await _httpClient.PostAsync(VenEl.AssistantMCP.Atlassian.Services.AtlassianProduct.Jira, "search/jql", payload, ct);

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("issues", out var issues) && issues.ValueKind == JsonValueKind.Array && issues.GetArrayLength() > 0)
            {
                var latestIssue = issues[0];
                var key = latestIssue.GetProperty("key").GetString();
                var fields = latestIssue.GetProperty("fields");
                var summary = fields.GetProperty("summary").GetString();
                var status = fields.GetProperty("status").GetProperty("name").GetString();
                var updatedStr = fields.GetProperty("updated").GetString();

                if (DateTime.TryParse(updatedStr, out var updatedDate) && updatedDate > _lastSeenUpdate)
                {
                    _lastSeenUpdate = updatedDate;
                    return $"[Jira Alert] Ticket {key} ('{summary}') assigned to you was recently updated to status '{status}'.";
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to poll Jira proactive alerts.");
            return null;
        }
    }
}
