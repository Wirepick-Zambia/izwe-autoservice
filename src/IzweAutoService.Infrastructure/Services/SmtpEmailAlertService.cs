using IzweAutoService.Application.Interfaces;
using IzweAutoService.Domain.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace IzweAutoService.Infrastructure.Services;

public class SmtpEmailAlertService : IEmailAlertService
{
    private readonly ISettingsRepository _settings;
    private readonly ILogger<SmtpEmailAlertService> _logger;

    public SmtpEmailAlertService(ISettingsRepository settings, ILogger<SmtpEmailAlertService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAlertAsync(string subject, string body)
    {
        var enabled = await _settings.GetValueAsync("AlertsEnabled");
        if (enabled != "true")
        {
            _logger.LogDebug("Email alerts are disabled");
            return;
        }

        var host = await _settings.GetValueAsync("SmtpHost") ?? "";
        var portStr = await _settings.GetValueAsync("SmtpPort") ?? "587";
        var username = await _settings.GetValueAsync("SmtpUsername") ?? "";
        var password = await _settings.GetValueAsync("SmtpPassword") ?? "";
        var useSsl = await _settings.GetValueAsync("SmtpUseSsl") == "true";
        var from = await _settings.GetValueAsync("AlertEmailFrom") ?? "";
        var to = await _settings.GetValueAsync("AlertEmailTo") ?? "";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("SMTP not fully configured — skipping alert");
            return;
        }

        if (!int.TryParse(portStr, out var port))
        {
            _logger.LogWarning("Invalid SMTP port '{Port}' — skipping alert", portStr);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        foreach (var recipient in to.Split(';', StringSplitOptions.RemoveEmptyEntries))
            message.To.Add(MailboxAddress.Parse(recipient.Trim()));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, useSsl);

            if (!string.IsNullOrEmpty(username))
                await client.AuthenticateAsync(username, password);

            await client.SendAsync(message);
            _logger.LogInformation("Alert email sent to {To}", to);
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true);
        }
    }
}
