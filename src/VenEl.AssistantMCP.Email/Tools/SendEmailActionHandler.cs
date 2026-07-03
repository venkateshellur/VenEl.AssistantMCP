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
        return null;
    }

    public async Task<string> HandleAsync(EmailCommandArgs args, CancellationToken ct)
    {
        var provider = _options.Provider;
        if (provider == EmailProviderType.Auto)
        {
            if (OperatingSystem.IsWindows())
                provider = EmailProviderType.Outlook;
            else
                provider = EmailProviderType.Graph;
        }

        switch (provider)
        {
            case EmailProviderType.Outlook:
                if (OperatingSystem.IsWindows())
                {
                    return await SendViaOutlookAsync(args, ct);
                }
                return "Outlook COM Interop is only supported on Windows.";
            case EmailProviderType.Graph:
                return await SendViaGraphApiAsync(args, ct);
            case EmailProviderType.Smtp:
            default:
                return await SendViaSmtpAsync(args, ct);
        }
    }

    private async Task<string> SendViaSmtpAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpServer)) return "SMTP Server is not configured.";
        if (string.IsNullOrWhiteSpace(_options.DefaultFromAddress)) return "Default From Address is not configured.";

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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private Task<string> SendViaOutlookAsync(EmailCommandArgs args, CancellationToken ct)
    {
        try
        {
            Type? outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) return Task.FromResult("Outlook is not installed on this machine.");
            
            dynamic outlookApp = Activator.CreateInstance(outlookType)!;
            dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
            mailItem.To = args.To;
            mailItem.Subject = args.Subject;
            
            if (args.IsHtml)
                mailItem.HTMLBody = args.Body;
            else
                mailItem.Body = args.Body;
            
            mailItem.Send();
            return Task.FromResult($"Email sent successfully to {args.To} (via Outlook COM Interop).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outlook COM Interop failed.");
            return Task.FromResult($"Failed to send via Outlook: {ex.Message}");
        }
    }

    private async Task<string> SendViaGraphApiAsync(EmailCommandArgs args, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.GraphApiToken))
        {
            return "Graph API Token is not configured in Email settings.";
        }

        try
        {
            var payload = new
            {
                message = new
                {
                    subject = args.Subject,
                    body = new
                    {
                        contentType = args.IsHtml ? "HTML" : "Text",
                        content = args.Body
                    },
                    toRecipients = new[]
                    {
                        new { emailAddress = new { address = args.To } }
                    }
                },
                saveToSentItems = "true"
            };

            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.GraphApiToken);
            
            var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("https://graph.microsoft.com/v1.0/me/sendMail", content, ct);
            if (response.IsSuccessStatusCode)
            {
                return $"Email sent successfully to {args.To} (via Microsoft Graph API).";
            }
            
            var errorResponse = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Graph API failed: {StatusCode} {Error}", response.StatusCode, errorResponse);
            return $"Failed to send via Graph API. Status: {response.StatusCode}. Details: {errorResponse}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Microsoft Graph API failed.");
            return $"Failed to send via Graph API: {ex.Message}";
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
