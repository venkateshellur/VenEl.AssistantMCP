namespace VenEl.AssistantMCP.Email.Configuration;

public enum EmailProviderType
{
    Auto,
    Smtp,
    Outlook,
    Graph
}

public class EmailOptions
{
    public const string SectionName = "Email";

    public EmailProviderType Provider { get; set; } = EmailProviderType.Auto;
    
    // SMTP Settings
    public string? SmtpServer { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public string? DefaultFromAddress { get; set; }
    
    // Graph API Settings
    public string? GraphApiToken { get; set; }
}
