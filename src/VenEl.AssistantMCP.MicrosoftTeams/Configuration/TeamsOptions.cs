namespace VenEl.AssistantMCP.MicrosoftTeams.Configuration;

public sealed class TeamsOptions
{
    public bool UseDefaultCredentials { get; set; } = true;
    public bool UseInteractiveBrowserAuth { get; set; }
    public bool UseDeviceCodeAuth { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? FallbackWebhookUrl { get; set; }
}
