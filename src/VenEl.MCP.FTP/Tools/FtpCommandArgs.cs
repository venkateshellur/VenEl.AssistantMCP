using System.Text.Json.Serialization;

namespace VenEl.MCP.FTP.Tools;

public class FtpCommandArgs
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonPropertyName("isSftp")]
    public bool IsSftp { get; set; }
    
    [JsonPropertyName("sourcePath")]
    public string SourcePath { get; set; } = string.Empty;
    
    [JsonPropertyName("destinationPath")]
    public string DestinationPath { get; set; } = string.Empty;
}
