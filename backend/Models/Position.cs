namespace TradingBot.Models;

public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid SymbolId { get; set; }
    public double Quantity { get; set; }
    public double AvgPrice { get; set; }
    public double UnrealizedPnl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public BistSymbol Symbol { get; set; } = null!;
}
