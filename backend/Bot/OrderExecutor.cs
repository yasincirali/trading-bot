using Microsoft.EntityFrameworkCore;
using TradingBot.Banks;
using TradingBot.Data;
using TradingBot.Indicators;
using TradingBot.Models;

namespace TradingBot.Bot;

public class OrderExecutor(AppDbContext db, BankAdapterFactory bankFactory, IConfiguration config, ILogger<OrderExecutor> logger)
{
    private static bool IsWithinTradingHours()
    {
        try
        {
            // Windows: "Turkey Standard Time", Linux: "Europe/Istanbul"
            var tzId = OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return turkeyTime.Hour >= 10 && turkeyTime.Hour < 18;
        }
        catch
        {
            // Fallback: UTC+3
            var turkeyTime = DateTime.UtcNow.AddHours(3);
            return turkeyTime.Hour >= 10 && turkeyTime.Hour < 18;
        }
    }

    public async Task ExecuteAsync(Guid userId, string ticker, AggregatedSignal signal, double maxOrderSizeTry, double dailyLossLimitTry)
    {
        bool paperTrade = config.GetValue<bool>("Trading:PaperTradingMode", true);

        if (!IsWithinTradingHours())
        {
            logger.LogDebug("Outside BIST trading hours, skipping {Ticker}", ticker);
            return;
        }

        // Daily loss check (simplified)
        var today = DateTime.UtcNow.Date;
        double dailyLoss = await db.Orders
            .Where(o => o.UserId == userId && o.CreatedAt >= today && o.Status == OrderStatus.FILLED && o.Type == OrderType.SELL)
            .SumAsync(o => o.Price * o.Quantity * 0.002);

        if (dailyLoss >= dailyLossLimitTry)
        {
            logger.LogWarning("Daily loss limit reached for user {UserId}", userId);
            return;
        }

        var symbol = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == ticker);
        if (symbol == null || symbol.LastPrice <= 0) return;

        var orderType = signal.Signal == SignalType.BUY ? OrderType.BUY : OrderType.SELL;
        int quantity = (int)(maxOrderSizeTry / symbol.LastPrice);
        if (quantity <= 0) return;

        if (orderType == OrderType.SELL)
        {
            var position = await db.Positions.FirstOrDefaultAsync(p => p.UserId == userId && p.SymbolId == symbol.Id);
            if (position == null || position.Quantity <= 0) return;
        }

        var adapter = bankFactory.GetDefault();
        var bankOrder = new BankOrder(ticker, orderType, quantity, symbol.LastPrice, paperTrade);

        var order = new Order
        {
            UserId = userId,
            SymbolId = symbol.Id,
            Type = orderType,
            Quantity = quantity,
            Price = symbol.LastPrice,
            Status = OrderStatus.PENDING,
            BankAdapter = adapter.Name,
            PaperTrade = paperTrade,
            Notes = $"Auto: confidence={signal.Confidence:F3}",
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        try
        {
            var result = await adapter.PlaceOrderAsync(bankOrder);
            order.Status = result.Status;
            order.Price = result.FilledPrice;
            order.FilledAt = result.FilledAt;
            await db.SaveChangesAsync();

            if (result.Status == OrderStatus.FILLED)
                await UpsertPositionAsync(userId, symbol.Id, orderType, quantity, result.FilledPrice);

            logger.LogInformation("{Type} {Qty} {Ticker} @ {Price:F2} — {Status}", orderType, quantity, ticker, result.FilledPrice, result.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Order execution failed for {Ticker}", ticker);
            order.Status = OrderStatus.FAILED;
            await db.SaveChangesAsync();
        }
    }

    private async Task UpsertPositionAsync(Guid userId, Guid symbolId, OrderType type, double qty, double price)
    {
        var pos = await db.Positions.FirstOrDefaultAsync(p => p.UserId == userId && p.SymbolId == symbolId);

        if (type == OrderType.BUY)
        {
            if (pos != null)
            {
                double totalQty = pos.Quantity + qty;
                pos.AvgPrice = (pos.Quantity * pos.AvgPrice + qty * price) / totalQty;
                pos.Quantity = totalQty;
                pos.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.Positions.Add(new Position { UserId = userId, SymbolId = symbolId, Quantity = qty, AvgPrice = price });
            }
        }
        else if (pos != null)
        {
            pos.Quantity -= qty;
            pos.UpdatedAt = DateTime.UtcNow;
            if (pos.Quantity <= 0) db.Positions.Remove(pos);
        }

        await db.SaveChangesAsync();
    }
}
