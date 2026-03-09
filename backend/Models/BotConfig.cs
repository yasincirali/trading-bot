namespace TradingBot.Models;

public class BotConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public bool Enabled { get; set; }
    public double ConfidenceThreshold { get; set; } = 0.65;
    public double MaxOrderSizeTry { get; set; } = 10000;
    public double DailyLossLimitTry { get; set; } = 5000;
    public int TickIntervalMs { get; set; } = 5000;
    public string[] Watchlist { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
