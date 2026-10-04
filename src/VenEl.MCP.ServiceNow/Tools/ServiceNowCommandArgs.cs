using System.Text.Json.Serialization;
namespace VenEl.MCP.ServiceNow.Tools;

public class ServiceNowCommandArgs
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }
    [JsonPropertyName("table")]
    public string? Table { get; set; }

    [JsonPropertyName("sysId")]
    public string? SysId { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;

    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}
