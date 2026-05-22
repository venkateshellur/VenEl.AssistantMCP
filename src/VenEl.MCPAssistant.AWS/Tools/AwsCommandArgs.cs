using System.Text.Json.Serialization;

namespace VenEl.MCPAssistant.AWS.Tools;

public sealed class AwsCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("bucketName")]
    public string? BucketName { get; set; }

    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }
}
