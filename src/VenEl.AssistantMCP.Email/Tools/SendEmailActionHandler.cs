using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using VenEl.AssistantMCP.Core.Dispatcher;
using VenEl.AssistantMCP.Email.Configuration;

namespace VenEl.AssistantMCP.Email.Tools;

public class SendEmailActionHandler : IActionHandler<EmailCommandArgs>
{
    private readonly EmailOptions _options;
    private readonly ILogger<SendEmailActionHandler> _logger;

    public SendEmailActionHandler(IOptions<EmailOptions> options, ILogger<SendEmailActionHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ActionName => "send_email";

    public string? Validate(EmailCommandArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.To)) return "Missing 'to' address.";
        if (string.IsNullOrWhiteSpace(args.Subject)) return "Missing 'subject'.";
        if (string.IsNullOrWhiteSpace(args.Body)) return "Missing 'body'.";
        
        if (string.IsNullOrWhiteSpace(_options.SmtpServer)) return "SMTP Server is not configured.";
        if (string.IsNullOrWhiteSpace(_options.DefaultFromAddress)) return "Default From Address is not configured.";

        return null;
    }

    public async Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            return await SendViaMailKitAsync(args, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MailKit failed to send email. Falling back to System.Net.Mail.");
            try
            {
                return await SendViaSystemNetMailAsync(args, ct);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "System.Net.Mail fallback also failed.");
                return $"Failed to send email. MailKit error: {ex.Message} | Fallback error: {fallbackEx.Message}";
            }
        }
    }

    private async Task<string> SendViaMailKitAsync(EmailCommandArgs args, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.DefaultFromAddress!));
        message.To.Add(MailboxAddress.Parse(args.To!));
        message.Subject = args.Subject;

        var bodyBuilder = new BodyBuilder();
        if (args.IsHtml)
            bodyBuilder.HtmlBody = args.Body;
        else
            bodyBuilder.TextBody = args.Body;

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(_options.SmtpServer!, _options.Port, 
            _options.EnableSsl ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        return $"Email sent successfully to {args.To} (via MailKit).";
    }

    private async Task<string> SendViaSystemNetMailAsync(EmailCommandArgs args, CancellationToken ct)
    {
        using var message = new System.Net.Mail.MailMessage();
        message.From = new System.Net.Mail.MailAddress(_options.DefaultFromAddress!);
        message.To.Add(args.To!);
        message.Subject = args.Subject;
        message.Body = args.Body;
        message.IsBodyHtml = args.IsHtml;

        using var client = new System.Net.Mail.SmtpClient(_options.SmtpServer, _options.Port);
        client.EnableSsl = _options.EnableSsl;
        
        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            client.Credentials = new System.Net.NetworkCredential(_options.Username, _options.Password);
        }

        await client.SendMailAsync(message, ct);

        return $"Email sent successfully to {args.To} (via System.Net.Mail fallback).";
    }
}
