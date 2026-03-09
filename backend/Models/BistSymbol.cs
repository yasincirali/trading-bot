namespace TradingBot.Models;

public class BistSymbol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Ticker { get; set; }
    public required string Name { get; set; }
    public required string Sector { get; set; }
    public double LastPrice { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Position> Positions { get; set; } = [];
    public ICollection<PriceCandle> Candles { get; set; } = [];
}
