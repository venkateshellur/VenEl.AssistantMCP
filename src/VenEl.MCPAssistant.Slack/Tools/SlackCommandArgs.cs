using System.Text.Json.Serialization;

namespace VenEl.MCPAssistant.Slack.Tools;

public sealed class SlackCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
