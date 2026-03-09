namespace TradingBot.Models;

public enum OrderType { BUY, SELL }
public enum OrderStatus { PENDING, FILLED, CANCELLED, FAILED }

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid SymbolId { get; set; }
    public OrderType Type { get; set; }
    public double Quantity { get; set; }
    public double Price { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PENDING;
    public string BankAdapter { get; set; } = "mock";
    public bool PaperTrade { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FilledAt { get; set; }

    public User User { get; set; } = null!;
    public BistSymbol Symbol { get; set; } = null!;
}
