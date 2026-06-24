using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.MicrosoftTeams.Configuration;

namespace VenEl.AssistantMCP.MicrosoftTeams.Tools;

public sealed class TeamsPostMessageActionHandler : IActionHandler<TeamsCommandArgs>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<TeamsOptions> _options;
    private readonly ILogger<TeamsPostMessageActionHandler> _logger;
    private readonly GraphServiceClient? _graphClient;

    public TeamsPostMessageActionHandler(
        IHttpClientFactory httpClientFactory,
        IOptions<TeamsOptions> options,
        ILogger<TeamsPostMessageActionHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;

        var opt = _options.Value;
        if (!string.IsNullOrWhiteSpace(opt.TenantId) && 
            !string.IsNullOrWhiteSpace(opt.ClientId) && 
            !string.IsNullOrWhiteSpace(opt.ClientSecret))
        {
            var credential = new ClientSecretCredential(opt.TenantId, opt.ClientId, opt.ClientSecret);
            _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        }
    }

    public string ActionName => "teams_post_message";

    public string? Validate(TeamsCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Message)) 
            return "Missing required parameter 'Message'.";
            
        // We need either Graph API configuration (TeamId and ChannelId) or a Webhook URL
        bool hasGraphConfig = _graphClient != null && !string.IsNullOrWhiteSpace(args.TeamId) && !string.IsNullOrWhiteSpace(args.ChannelId);
        bool hasWebhook = !string.IsNullOrWhiteSpace(args.WebhookUrl) || !string.IsNullOrWhiteSpace(_options.Value.FallbackWebhookUrl);

        if (!hasGraphConfig && !hasWebhook)
            return "Missing sufficient configuration to send message (Graph API auth+TeamId/ChannelId or Webhook URL).";

        return null;
    }

    public async Task<string> HandleAsync(TeamsCommandArgs args, CancellationToken ct)
    {
        if (_graphClient != null && !string.IsNullOrWhiteSpace(args.TeamId) && !string.IsNullOrWhiteSpace(args.ChannelId))
        {
            try
            {
                _logger.LogDebug("Attempting to post message via Microsoft Graph API");
                var chatMessage = new ChatMessage
                {
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = args.Message
                    }
                };
                
                await _graphClient.Teams[args.TeamId].Channels[args.ChannelId].Messages
                    .PostAsync(chatMessage, cancellationToken: ct);
                    
                return "Message posted to Microsoft Teams via Graph API successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post message via Graph API. Falling back to Webhook if configured.");
            }
        }

        // Fallback to Webhook
        var webhookUrl = !string.IsNullOrWhiteSpace(args.WebhookUrl) ? args.WebhookUrl : _options.Value.FallbackWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return "[ERROR] Graph API failed/unconfigured and no fallback webhook URL is available.";
        }

        _logger.LogDebug("Posting message to Teams fallback webhook");
        var payload = new { text = args.Message };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = _httpClientFactory.CreateClient("TeamsWebhookClient");
        var response = await client.PostAsync(webhookUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            return $"[ERROR] Failed to post to Teams Webhook: {response.StatusCode} - {err}";
        }

        return "Message posted to Microsoft Teams Webhook successfully.";
    }
}
