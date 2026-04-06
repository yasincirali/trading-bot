using System.Net.Http.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using TradingBot.Data;
using TradingBot.Indicators;
using TradingBot.Models;

namespace TradingBot.Services;

public record TradeSignalInfo(
    string Ticker,
    AggregatedSignal Signal,
    double Price,
    int EstimatedQuantity,
    bool PaperTrade);

public record OrderFilledInfo(
    string Ticker,
    OrderType OrderType,
    int Quantity,
    double FilledPrice,
    string BankAdapter,
    bool PaperTrade);

public class NotificationService(
    AppDbContext db,
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<NotificationService> logger)
{
    // ── Public API ────────────────────────────────────────────────────────────

    public async Task SendTradeSignalAsync(Guid userId, TradeSignalInfo info)
    {
        var text = BuildSignalText(info);
        var html = BuildSignalHtml(info);
        await DispatchAsync(userId, "BIST Sinyal", text, html, NotificationChannel.TELEGRAM);
    }

    public async Task SendOrderFilledAsync(Guid userId, OrderFilledInfo info)
    {
        var text = BuildOrderText(info);
        var html = BuildOrderHtml(info);
        await DispatchAsync(userId, "Emir Gerçekleşti", text, html, NotificationChannel.EMAIL);
    }

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
            success = channel switch
            {
                NotificationChannel.SMS => await SendSmsAsync(userId, message),
                NotificationChannel.TELEGRAM => await SendTelegramAsync(message),
                _ => await SendEmailAsync(userId, "BIST Trading Bot", $"<p>{message.Replace("\n", "<br>")}</p>", message),
            };

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

    // ── Dispatch to all configured channels ──────────────────────────────────

    private async Task DispatchAsync(Guid userId, string subject, string plainText, string htmlBody, NotificationChannel preferredChannel)
    {
        var telegramToken = config["Telegram:BotToken"];
        var telegramConfigured = !string.IsNullOrEmpty(telegramToken) && !telegramToken.StartsWith("your-");

        var smtpUser = config["Smtp:User"];
        var emailConfigured = !string.IsNullOrEmpty(smtpUser) && smtpUser != "your@gmail.com";

        var tasks = new List<Task<(bool success, NotificationChannel channel)>>();

        if (telegramConfigured)
            tasks.Add(SendTelegramAsync(plainText).ContinueWith(t => (t.Result, NotificationChannel.TELEGRAM)));

        if (emailConfigured)
            tasks.Add(SendEmailAsync(userId, subject, htmlBody, plainText).ContinueWith(t => (t.Result, NotificationChannel.EMAIL)));

        if (tasks.Count == 0)
        {
            // Mock — log to console
            logger.LogInformation("[Notification Mock]\n{Text}", plainText);
            await SaveNotificationAsync(userId, NotificationChannel.TELEGRAM, plainText, true);
            return;
        }

        var results = await Task.WhenAll(tasks);
        foreach (var (success, channel) in results)
            await SaveNotificationAsync(userId, channel, plainText, success);
    }

    private async Task SaveNotificationAsync(Guid userId, NotificationChannel channel, string message, bool success)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Channel = channel,
            Message = message,
            Status = success ? NotificationStatus.SENT : NotificationStatus.FAILED,
            SentAt = success ? DateTime.UtcNow : null,
        });
        await db.SaveChangesAsync();
    }

    // ── Telegram ──────────────────────────────────────────────────────────────

    private async Task<bool> SendTelegramAsync(string message)
    {
        var token = config["Telegram:BotToken"];
        var chatId = config["Telegram:ChatId"];

        if (string.IsNullOrEmpty(token) || token.StartsWith("your-"))
        {
            logger.LogInformation("[Telegram Mock]\n{Message}", message);
            return true;
        }

        try
        {
            var client = httpClientFactory.CreateClient("Telegram");
            var url = $"https://api.telegram.org/bot{token}/sendMessage";
            var payload = new { chat_id = chatId, text = message, parse_mode = "HTML" };
            var response = await client.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                logger.LogWarning("[Telegram] Send failed: {Error}", err);
                return false;
            }
            logger.LogInformation("[Telegram] Sent to chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Telegram] Exception");
            return false;
        }
    }

    // ── Email ─────────────────────────────────────────────────────────────────

    private async Task<bool> SendEmailAsync(Guid userId, string subject, string htmlBody, string fallbackText)
    {
        var user = await db.Users.FindAsync(userId);
        var to = user?.Email;
        if (string.IsNullOrEmpty(to)) return false;

        var host = config["Smtp:Host"];
        var smtpUser = config["Smtp:User"];

        if (string.IsNullOrEmpty(host) || smtpUser == "your@gmail.com")
        {
            logger.LogInformation("[Email Mock] To: {Email}\n{Text}", to, fallbackText);
            return true;
        }

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(config["Smtp:From"] ?? smtpUser));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = htmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, config.GetValue<int>("Smtp:Port", 587), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(smtpUser, config["Smtp:Pass"]);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
        logger.LogInformation("[Email] Sent to {Email}", to);
        return true;
    }

    // ── SMS (Twilio) ──────────────────────────────────────────────────────────

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
            logger.LogInformation("[SMS Mock] To: {Phone} | {Message}", normalized, message);
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

    // ── Message Builders ──────────────────────────────────────────────────────

    private static string BuildSignalText(TradeSignalInfo info)
    {
        var s = info.Signal;
        var dir = s.Signal == SignalType.BUY ? "🟢 ALIŞ" : "🔴 SATIŞ";
        var estimated = info.EstimatedQuantity * info.Price;
        var tz = GetTurkeyTime();

        var rsiIcon = SignalIcon(s.Rsi.Signal);
        var macdIcon = SignalIcon(s.Macd.Signal);
        var bollIcon = SignalIcon(s.Bollinger.Signal);
        var emaIcon = SignalIcon(s.Ema.Signal);

        return $"""
📊 <b>BIST İŞLEM SİNYALİ</b>

{dir} SİNYALİ — <b>{info.Ticker}</b>
⏰ {tz:HH:mm:ss} (TR Saati)

💰 Fiyat: {info.Price:N2} ₺
📦 Tahmini Lot: {info.EstimatedQuantity:N0} adet
💵 Tahmini Tutar: ~{estimated:N2} ₺

📈 <b>Göstergeler:</b>
• RSI    (30%): {rsiIcon} {SignalTR(s.Rsi.Signal)} — Güç: %{s.Rsi.Strength * 100:F0} | Değer: {s.Rsi.Value:F1}
• MACD   (30%): {macdIcon} {SignalTR(s.Macd.Signal)} — Güç: %{s.Macd.Strength * 100:F0} | Değer: {s.Macd.Value:F4}
• Bollinger(20%): {bollIcon} {SignalTR(s.Bollinger.Signal)} — Güç: %{s.Bollinger.Strength * 100:F0} | Değer: {s.Bollinger.Value:F2}
• EMA    (20%): {emaIcon} {SignalTR(s.Ema.Signal)} — Güç: %{s.Ema.Strength * 100:F0} | Değer: {s.Ema.Value:F2}

🎯 Güven Skoru: %{s.Confidence * 100:F0}
🔄 Mod: {(info.PaperTrade ? "Kağıt İşlem (Paper Trade)" : "GERÇEK İŞLEM")}
""";
    }

    private static string BuildOrderText(OrderFilledInfo info)
    {
        var dir = info.OrderType == OrderType.BUY ? "🟢 ALIŞ" : "🔴 SATIŞ";
        var total = info.Quantity * info.FilledPrice;
        var tz = GetTurkeyTime();

        return $"""
✅ <b>EMİR GERÇEKLEŞTİ</b>

{dir}: <b>{info.Ticker}</b>
📦 {info.Quantity:N0} adet @ {info.FilledPrice:N2} ₺
💵 Toplam: {total:N2} ₺
🏦 Banka: {info.BankAdapter}
🔄 Mod: {(info.PaperTrade ? "Kağıt İşlem" : "GERÇEK İŞLEM")}
⏰ {tz:HH:mm:ss} (TR Saati)
""";
    }

    private static string BuildSignalHtml(TradeSignalInfo info)
    {
        var s = info.Signal;
        var color = s.Signal == SignalType.BUY ? "#16a34a" : "#dc2626";
        var dir = s.Signal == SignalType.BUY ? "ALIŞ" : "SATIŞ";
        var estimated = info.EstimatedQuantity * info.Price;
        var tz = GetTurkeyTime();

        return $"""
<!DOCTYPE html>
<html><body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;background:#f9fafb">
  <div style="background:white;border-radius:12px;padding:24px;box-shadow:0 2px 8px rgba(0,0,0,.1)">
    <h2 style="margin:0 0 16px;color:#111827">📊 BIST İŞLEM SİNYALİ</h2>
    <div style="background:{color};color:white;border-radius:8px;padding:12px 16px;margin-bottom:16px">
      <strong style="font-size:18px">{dir} SİNYALİ — {info.Ticker}</strong><br>
      <span style="opacity:.9">⏰ {tz:HH:mm:ss} (TR Saati)</span>
    </div>
    <table style="width:100%;border-collapse:collapse;margin-bottom:16px">
      <tr style="background:#f3f4f6">
        <td style="padding:8px 12px;font-weight:bold">💰 Fiyat</td>
        <td style="padding:8px 12px">{info.Price:N2} ₺</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;font-weight:bold">📦 Tahmini Lot</td>
        <td style="padding:8px 12px">{info.EstimatedQuantity:N0} adet</td>
      </tr>
      <tr style="background:#f3f4f6">
        <td style="padding:8px 12px;font-weight:bold">💵 Tahmini Tutar</td>
        <td style="padding:8px 12px">~{estimated:N2} ₺</td>
      </tr>
    </table>
    <h3 style="margin:0 0 8px;color:#374151">📈 Göstergeler</h3>
    <table style="width:100%;border-collapse:collapse;font-size:14px">
      <thead>
        <tr style="background:#e5e7eb">
          <th style="padding:6px 10px;text-align:left">Gösterge</th>
          <th style="padding:6px 10px;text-align:left">Sinyal</th>
          <th style="padding:6px 10px;text-align:left">Güç</th>
          <th style="padding:6px 10px;text-align:left">Değer</th>
        </tr>
      </thead>
      <tbody>
        {IndicatorRow("RSI (30%)", s.Rsi.Signal, s.Rsi.Strength, (double)(s.Rsi.Value ?? 0.0), "F1")}
        {IndicatorRow("MACD (30%)", s.Macd.Signal, s.Macd.Strength, (double)(s.Macd.Value ?? 0.0), "F4")}
        {IndicatorRow("Bollinger (20%)", s.Bollinger.Signal, s.Bollinger.Strength, (double)(s.Bollinger.Value ?? 0.0), "F2")}
        {IndicatorRow("EMA (20%)", s.Ema.Signal, s.Ema.Strength, (double)(s.Ema.Value ?? 0.0), "F2")}
      </tbody>
    </table>
    <div style="margin-top:16px;padding:12px;background:#f0fdf4;border-radius:8px;display:flex;justify-content:space-between">
      <span>🎯 <strong>Güven Skoru: %{s.Confidence * 100:F0}</strong></span>
      <span>🔄 {(info.PaperTrade ? "Kağıt İşlem" : "<strong style='color:#dc2626'>GERÇEK İŞLEM</strong>")}</span>
    </div>
  </div>
</body></html>
""";
    }

    private static string BuildOrderHtml(OrderFilledInfo info)
    {
        var color = info.OrderType == OrderType.BUY ? "#16a34a" : "#dc2626";
        var dir = info.OrderType == OrderType.BUY ? "ALIŞ" : "SATIŞ";
        var total = info.Quantity * info.FilledPrice;
        var tz = GetTurkeyTime();

        return $"""
<!DOCTYPE html>
<html><body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;background:#f9fafb">
  <div style="background:white;border-radius:12px;padding:24px;box-shadow:0 2px 8px rgba(0,0,0,.1)">
    <h2 style="margin:0 0 16px;color:#111827">✅ EMİR GERÇEKLEŞTİ</h2>
    <div style="background:{color};color:white;border-radius:8px;padding:12px 16px;margin-bottom:16px">
      <strong style="font-size:18px">{dir} — {info.Ticker}</strong><br>
      <span style="opacity:.9">⏰ {tz:HH:mm:ss} (TR Saati)</span>
    </div>
    <table style="width:100%;border-collapse:collapse">
      <tr style="background:#f3f4f6">
        <td style="padding:8px 12px;font-weight:bold">📦 Miktar</td>
        <td style="padding:8px 12px">{info.Quantity:N0} adet</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;font-weight:bold">💰 Gerçekleşme Fiyatı</td>
        <td style="padding:8px 12px">{info.FilledPrice:N2} ₺</td>
      </tr>
      <tr style="background:#f3f4f6">
        <td style="padding:8px 12px;font-weight:bold">💵 Toplam Tutar</td>
        <td style="padding:8px 12px;font-weight:bold">{total:N2} ₺</td>
      </tr>
      <tr>
        <td style="padding:8px 12px;font-weight:bold">🏦 Banka/Adaptör</td>
        <td style="padding:8px 12px">{info.BankAdapter}</td>
      </tr>
      <tr style="background:#f3f4f6">
        <td style="padding:8px 12px;font-weight:bold">🔄 Mod</td>
        <td style="padding:8px 12px">{(info.PaperTrade ? "Kağıt İşlem (Paper Trade)" : "<strong style='color:#dc2626'>GERÇEK İŞLEM</strong>")}</td>
      </tr>
    </table>
  </div>
</body></html>
""";
    }

    private static string IndicatorRow(string name, SignalType signal, double strength, double value, string fmt)
    {
        var color = signal == SignalType.BUY ? "#16a34a" : signal == SignalType.SELL ? "#dc2626" : "#6b7280";
        return $"<tr><td style='padding:6px 10px'>{name}</td>"
             + $"<td style='padding:6px 10px;color:{color};font-weight:bold'>{SignalTR(signal)}</td>"
             + $"<td style='padding:6px 10px'>%{strength * 100:F0}</td>"
             + $"<td style='padding:6px 10px'>{value.ToString(fmt)}</td></tr>";
    }

    private static string SignalTR(SignalType s) => s switch
    {
        SignalType.BUY => "ALIŞ",
        SignalType.SELL => "SATIŞ",
        _ => "NÖTR",
    };

    private static string SignalIcon(SignalType s) => s switch
    {
        SignalType.BUY => "🟢",
        SignalType.SELL => "🔴",
        _ => "⚪",
    };

    private static DateTime GetTurkeyTime()
    {
        try
        {
            var tzId = OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul";
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(tzId));
        }
        catch { return DateTime.UtcNow.AddHours(3); }
    }
}
