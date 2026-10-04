using System.Text.Json.Serialization;

namespace VenEl.MCP.ServiceNow.Configuration;

public class ServiceNowOptions
{
    public const string SectionName = "ServiceNow";

    [JsonPropertyName("instanceUrl")]
    public string? InstanceUrl { get; set; }

    [JsonPropertyName("authMode")]
    public string? AuthMode { get; set; } = "Basic"; // Basic or OAuth

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("defaultApiVersion")]
    public string DefaultApiVersion { get; set; } = "v2";
}
