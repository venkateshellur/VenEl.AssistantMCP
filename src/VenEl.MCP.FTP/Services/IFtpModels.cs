namespace VenEl.MCP.FTP.Services;

public class FtpItem
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime Modified { get; set; }
}

public class FtpConnectionOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsSftp { get; set; }
}
