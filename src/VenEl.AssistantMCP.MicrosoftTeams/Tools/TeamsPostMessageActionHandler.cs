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
using VenEl.AssistantMCP.Core.Security;

namespace VenEl.AssistantMCP.MicrosoftTeams.Tools;

public sealed class TeamsPostMessageActionHandler : IActionHandler<TeamsCommandArgs>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<TeamsOptions> _options;
    private readonly SecretManager _secretManager;
    private readonly ILogger<TeamsPostMessageActionHandler> _logger;
    private GraphServiceClient? _graphClient;
    private bool _graphClientInitialized;

    public TeamsPostMessageActionHandler(
        IHttpClientFactory httpClientFactory,
        IOptions<TeamsOptions> options,
        SecretManager secretManager,
        ILogger<TeamsPostMessageActionHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _secretManager = secretManager;
        _logger = logger;
    }

    private async Task<GraphServiceClient?> GetGraphClientAsync(CancellationToken ct)
    {
        if (_graphClientInitialized) return _graphClient;
        _graphClientInitialized = true;

        var opt = _options.Value;
        var clientSecret = await _secretManager.ResolveSecretAsync(opt.ClientSecret, ct);
        var clientId = await _secretManager.ResolveSecretAsync(opt.ClientId, ct);
        var tenantId = await _secretManager.ResolveSecretAsync(opt.TenantId, ct);

        if (!string.IsNullOrWhiteSpace(tenantId) && 
            !string.IsNullOrWhiteSpace(clientId) && 
            !string.IsNullOrWhiteSpace(clientSecret))
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        }
        else if (opt.UseInteractiveBrowserAuth && !string.IsNullOrWhiteSpace(clientId))
        {
            var interactiveOptions = new InteractiveBrowserCredentialOptions
            {
                TenantId = tenantId,
                ClientId = clientId
            };
            var credential = new InteractiveBrowserCredential(interactiveOptions);
            _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        }
        else if (opt.UseDeviceCodeAuth && !string.IsNullOrWhiteSpace(clientId))
        {
            var deviceCodeOptions = new DeviceCodeCredentialOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
                DeviceCodeCallback = (code, cancellation) =>
                {
                    _logger.LogWarning(code.Message);
                    return Task.FromResult(0);
                }
            };
            var credential = new DeviceCodeCredential(deviceCodeOptions);
            _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        }
        else if (opt.UseDefaultCredentials)
        {
            var credential = new DefaultAzureCredential();
            _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        }

        return _graphClient;
    }

    public string ActionName => "teams_post_message";

    public string? Validate(TeamsCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Message)) 
            return "Missing required parameter 'Message'.";
            
        return null; // Defer complete validation to HandleAsync since we need to await secrets
    }

    public async Task<string> HandleAsync(TeamsCommandArgs args, CancellationToken ct)
    {
        var graphClient = await GetGraphClientAsync(ct);
        
        bool hasGraphConfig = graphClient != null && !string.IsNullOrWhiteSpace(args.TeamId) && !string.IsNullOrWhiteSpace(args.ChannelId);
        
        var rawFallback = _options.Value.FallbackWebhookUrl;
        var rawWebhook = !string.IsNullOrWhiteSpace(args.WebhookUrl) ? args.WebhookUrl : rawFallback;
        var webhookUrl = await _secretManager.ResolveSecretAsync(rawWebhook, ct);

        bool hasWebhook = !string.IsNullOrWhiteSpace(webhookUrl);

        if (!hasGraphConfig && !hasWebhook)
            return "Missing sufficient configuration to send message (Graph API auth+TeamId/ChannelId or Webhook URL).";

        if (hasGraphConfig)
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
                
                await graphClient!.Teams[args.TeamId].Channels[args.ChannelId].Messages
                    .PostAsync(chatMessage, cancellationToken: ct);
                    
                return "Message posted to Microsoft Teams via Graph API successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post message via Graph API. Falling back to Webhook if configured.");
            }
        }

        // Fallback to Webhook
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
