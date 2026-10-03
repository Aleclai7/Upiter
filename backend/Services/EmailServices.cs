using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Upiter.Api.Services;

public class EmailAlertService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailAlertService> _logger;

    public EmailAlertService(IConfiguration config, ILogger<EmailAlertService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAlertAsync(string targetName, bool isUp, DateTime checkedAt)
    {
        var fromAddress = _config["Email:FromAddress"];
        var appPassword = _config["Email:AppPassword"];
        var toAddress = _config["Email:ToAddress"];

        if (fromAddress is null || appPassword is null || toAddress is null)
        {
            _logger.LogError("Email configuration is missing. Please check your appsettings.json or environment variables.");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = $"[Upiter] {targetName} is now {(isUp ? "UP" : "DOWN")}";
        message.Body = new TextPart("plain")
        {
            Text = $"{targetName} changed status to {(isUp ? "UP" : "DOWN")} at {checkedAt:u}."
        };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(fromAddress, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Alert email sent for {Target}", targetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert email for {Target}", targetName);
        }
    }
}
