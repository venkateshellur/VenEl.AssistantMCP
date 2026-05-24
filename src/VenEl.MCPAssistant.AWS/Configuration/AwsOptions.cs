namespace VenEl.MCPAssistant.AWS.Configuration;

public class AwsOptions
{
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Region { get; set; }
    public bool AllowResourceDeletion { get; set; } = false;
}
