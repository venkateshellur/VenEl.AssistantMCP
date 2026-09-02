using System;

namespace VenEl.AssistantMCP.FTP.Services;

public class FtpServiceFactory
{
    public IFtpService CreateService(FtpConnectionOptions options)
    {
        if (options.IsSftp)
        {
            return new SftpService();
        }
        return new FluentFtpService();
    }
}
