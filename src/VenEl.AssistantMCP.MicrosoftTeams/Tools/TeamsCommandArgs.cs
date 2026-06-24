using System.Text.Json.Serialization;

namespace VenEl.AssistantMCP.MicrosoftTeams.Tools;

public sealed class TeamsCommandArgs
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("teamId")]
    public string? TeamId { get; set; }

    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }
}
