using System.Text.Json.Serialization;

namespace VenEl.AssistantMCP.Email.Tools;

public class EmailCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("isHtml")]
    public bool IsHtml { get; set; } = false;
}
