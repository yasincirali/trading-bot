using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using TradingBot.Data;
using TradingBot.Models;

namespace TradingBot.Services;

public class NotificationService(AppDbContext db, IConfiguration config, ILogger<NotificationService> logger)
{
    public async Task SendAsync(Guid userId, NotificationChannel channel, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Channel = channel,
            Message = message,
            Status = NotificationStatus.PENDING,
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        bool success;
        try
        {
            success = channel == NotificationChannel.SMS
                ? await SendSmsAsync(userId, message)
                : await SendEmailAsync(userId, message);

            notification.Status = success ? NotificationStatus.SENT : NotificationStatus.FAILED;
            if (success) notification.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Channel} notification", channel);
            notification.Status = NotificationStatus.FAILED;
        }

        await db.SaveChangesAsync();
    }

    private async Task<bool> SendSmsAsync(Guid userId, string message)
    {
        var user = await db.Users.FindAsync(userId);
        var phone = user?.Phone;
        if (string.IsNullOrEmpty(phone))
        {
            logger.LogWarning("[SMS] No phone for user {UserId}", userId);
            return false;
        }

        var normalized = phone.StartsWith('+') ? phone : $"+90{phone.TrimStart('0')}";

        var sid = config["Twilio:AccountSid"];
        var token = config["Twilio:AuthToken"];
        var from = config["Twilio:PhoneFrom"];

        if (string.IsNullOrEmpty(sid) || sid.StartsWith("ACxx"))
        {
            logger.LogInformation("[SMS] (Mock) To: {Phone} | {Message}", normalized, message);
            return true;
        }

        TwilioClient.Init(sid, token);
        var msg = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(from),
            to: new Twilio.Types.PhoneNumber(normalized));
        logger.LogInformation("[SMS] Sent to {Phone}, SID: {Sid}", normalized, msg.Sid);
        return true;
    }

    private async Task<bool> SendEmailAsync(Guid userId, string message)
    {
        var user = await db.Users.FindAsync(userId);
        var to = user?.Email;
        if (string.IsNullOrEmpty(to)) return false;

        var host = config["Smtp:Host"];
        var smtpUser = config["Smtp:User"];

        if (string.IsNullOrEmpty(host) || smtpUser == "your@gmail.com")
        {
            logger.LogInformation("[Email] (Mock) To: {Email} | {Message}", to, message);
            return true;
        }

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(config["Smtp:From"] ?? smtpUser));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = "BIST Trading Bot — Notification";
        email.Body = new TextPart("html") { Text = $"<p>{message.Replace("\n", "<br>")}</p>" };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, config.GetValue<int>("Smtp:Port", 587), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(smtpUser, config["Smtp:Pass"]);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
        return true;
    }
}
