using System.Text.Json.Serialization;

namespace VenEl.AssistantMCP.GCP.Tools;

public sealed class GcpCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("bucketName")]
    public string? BucketName { get; set; }

    [JsonPropertyName("zone")]
    public string? Zone { get; set; }
}
