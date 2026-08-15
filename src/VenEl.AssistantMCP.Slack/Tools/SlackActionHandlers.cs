using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Slack.Configuration;
using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.Slack.Tools;

public sealed class SlackPostMessageActionHandler(
    IHttpClientFactory httpClientFactory,
    IOptions<SlackOptions> options,
    SecretManager secretManager,
    ILogger<SlackPostMessageActionHandler> logger) : IActionHandler<SlackCommandArgs>
{
    public string ActionName => "slack_post_message";

    public string? Validate(SlackCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Message)) return "Missing required parameter 'Message'.";
        if (string.IsNullOrWhiteSpace(args.WebhookUrl) && string.IsNullOrWhiteSpace(options.Value.WebhookUrl))
            return "Missing WebhookUrl in arguments or configuration.";
        return null;
    }

    public async Task<string> HandleAsync(SlackCommandArgs args, CancellationToken ct)
    {
        var rawUrl = !string.IsNullOrWhiteSpace(args.WebhookUrl) ? args.WebhookUrl : options.Value.WebhookUrl;
        var url = await secretManager.ResolveSecretAsync(rawUrl, ct);
        logger.LogDebug("Posting message to Slack webhook");

        var payload = new { text = args.Message };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = httpClientFactory.CreateClient("SlackClient");
        var response = await client.PostAsync(url, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            return $"[ERROR] Failed to post to Slack: {response.StatusCode} - {err}";
        }

        return "Message posted to Slack successfully.";
    }
}
