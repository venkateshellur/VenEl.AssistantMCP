using System.ComponentModel;
using System.Text.Json.Serialization;

namespace VenEl.AssistantMCP.WebAutomator.Tools;

public sealed class WebAutomatorArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("selector")]
    public string? Selector { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("script")]
    public string? Script { get; set; }
}
