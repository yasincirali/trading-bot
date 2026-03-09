namespace TradingBot.Models;

public enum NotificationChannel { SMS, EMAIL }
public enum NotificationStatus { PENDING, SENT, FAILED }

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public required string Message { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.PENDING;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
