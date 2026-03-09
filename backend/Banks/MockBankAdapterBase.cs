using TradingBot.Models;

namespace TradingBot.Banks;

public abstract class MockBankAdapterBase : IBankAdapter
{
    public abstract string Name { get; }

    private double _balance = 100_000; // 100k TRY
    private readonly Dictionary<string, (double Quantity, double AvgPrice)> _positions = new();
    private readonly Random _rng = new();

    public Task<BankBalance> GetBalanceAsync()
    {
        double posValue = _positions.Values.Sum(p => p.Quantity * p.AvgPrice);
        return Task.FromResult(new BankBalance(_balance, _balance + posValue));
    }

    public async Task<BankOrderResult> PlaceOrderAsync(BankOrder order)
    {
        // Simulate network latency 200–800ms
        await Task.Delay(_rng.Next(200, 800));

        double slippage = ((_rng.NextDouble() - 0.5) * 0.002);
        double filledPrice = order.Price * (1 + slippage);
        double commission = filledPrice * order.Quantity * 0.002; // 0.2%
        string orderId = $"{Name.ToUpperInvariant()}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        if (order.Type == OrderType.BUY)
        {
            double totalCost = filledPrice * order.Quantity + commission;
            if (_balance < totalCost)
                return new BankOrderResult(orderId, OrderStatus.FAILED, 0, DateTime.UtcNow, 0);

            _balance -= totalCost;
            if (_positions.TryGetValue(order.SymbolTicker, out var existing))
            {
                double totalQty = existing.Quantity + order.Quantity;
                double newAvg = (existing.Quantity * existing.AvgPrice + order.Quantity * filledPrice) / totalQty;
                _positions[order.SymbolTicker] = (totalQty, newAvg);
            }
            else
            {
                _positions[order.SymbolTicker] = (order.Quantity, filledPrice);
            }
        }
        else // SELL
        {
            if (!_positions.TryGetValue(order.SymbolTicker, out var existing) || existing.Quantity < order.Quantity)
                return new BankOrderResult(orderId, OrderStatus.FAILED, 0, DateTime.UtcNow, 0);

            _balance += filledPrice * order.Quantity - commission;
            double remaining = existing.Quantity - order.Quantity;
            if (remaining <= 0) _positions.Remove(order.SymbolTicker);
            else _positions[order.SymbolTicker] = (remaining, existing.AvgPrice);
        }

        Console.WriteLine($"[{Name}] {order.Type} {order.Quantity} {order.SymbolTicker} @ {filledPrice:F2} TRY (commission: {commission:F2})");
        return new BankOrderResult(orderId, OrderStatus.FILLED, filledPrice, DateTime.UtcNow, commission);
    }

    public Task<IEnumerable<BankPosition>> GetPositionsAsync()
    {
        var positions = _positions.Select(kvp => new BankPosition(
            kvp.Key, kvp.Value.Quantity, kvp.Value.AvgPrice, kvp.Value.AvgPrice, 0));
        return Task.FromResult(positions);
    }

    public async Task<bool> CancelOrderAsync(string orderId)
    {
        await Task.Delay(100);
        return true;
    }
}
