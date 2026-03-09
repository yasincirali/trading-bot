namespace TradingBot.Models;

public enum UserRole { USER, ADMIN }

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.USER;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Position> Positions { get; set; } = [];
    public BotConfig? BotConfig { get; set; }
    public ICollection<Notification> Notifications { get; set; } = [];
}
