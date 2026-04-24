using DomainViewer.Core.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace DomainViewer.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        var smtpSection = configuration.GetSection("Smtp");
        _smtpServer = smtpSection["Server"] ?? "smtp.gmail.com";
        _smtpPort = int.TryParse(smtpSection["Port"], out var port) ? port : 587;
        _smtpUsername = smtpSection["Username"] ?? throw new InvalidOperationException("SMTP Username is not configured");
        _smtpPassword = smtpSection["Password"] ?? throw new InvalidOperationException("SMTP Password is not configured");
        _fromEmail = smtpSection["FromEmail"] ?? _smtpUsername;
        _fromName = smtpSection["FromName"] ?? "Domain Viewer";
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = isHtml ? body : null,
            TextBody = isHtml ? null : body
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_smtpUsername, _smtpPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
