using System.ComponentModel;
using System.Text.Json.Serialization;

namespace VenEl.MCPAssistant.Bitwarden.Tools;

public class BitwardenCommandArgs
{
    [Description("The action to perform (e.g. 'bitwarden_get_secret')")]
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [Description("The exact name or Secret ID of the item in the Bitwarden vault")]
    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }
}
