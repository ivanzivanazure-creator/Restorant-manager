using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.Services;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FromAddress { get; set; } = default!;
    public string FromName { get; set; } = "Restaurant SaaS";
}

/// <summary>Email is fully wired (SMTP via MailKit). SMS/Push are Phase 2: the INotificationSender contract
/// already models them (NotificationChannelKind), so adding Twilio/FCM adapters later needs no interface
/// change — see docs/ROADMAP.md.</summary>
public sealed class NotificationSenderService(IOptions<SmtpOptions> options, ILogger<NotificationSenderService> logger) : INotificationSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(NotificationChannelKind channel, string recipient, string subject, string body, CancellationToken ct)
    {
        switch (channel)
        {
            case NotificationChannelKind.Email:
                await SendEmailAsync(recipient, subject, body, ct);
                break;
            case NotificationChannelKind.Sms:
            case NotificationChannelKind.Push:
                logger.LogWarning("{Channel} notifications are not yet implemented (Phase 2); dropped message to {Recipient}: {Subject}",
                    channel, recipient, subject);
                break;
        }
    }

    private async Task SendEmailAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
