namespace TradingBot.Models;

public class PriceCandle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SymbolId { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public double Volume { get; set; }
    public DateTime Timestamp { get; set; }

    public BistSymbol Symbol { get; set; } = null!;
}
