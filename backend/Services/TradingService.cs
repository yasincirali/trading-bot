using Microsoft.EntityFrameworkCore;
using TradingBot.Banks;
using TradingBot.Data;
using TradingBot.DTOs;
using TradingBot.Models;

namespace TradingBot.Services;

public class TradingService(AppDbContext db, BankAdapterFactory bankFactory, IConfiguration config)
{
    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(Guid userId, int limit = 50)
    {
        var orders = await db.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Symbol)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync();
        return orders.Select(ToOrderDto);
    }

    public async Task<OrderDto> PlaceOrderAsync(Guid userId, PlaceOrderRequest req)
    {
        var symbol = await db.BistSymbols.FirstOrDefaultAsync(s => s.Ticker == req.Ticker.ToUpper())
            ?? throw new AppException($"Symbol not found: {req.Ticker}", 404);

        if (req.Quantity <= 0) throw new AppException("Quantity must be positive");

        bool paperTrade = config.GetValue<bool>("Trading:PaperTradingMode", true);
        var adapter = bankFactory.Get(req.BankAdapter);

        var order = new Order
        {
            UserId = userId,
            SymbolId = symbol.Id,
            Type = req.Type,
            Quantity = req.Quantity,
            Price = symbol.LastPrice,
            BankAdapter = adapter.Name,
            PaperTrade = paperTrade,
            Notes = "Manual order",
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        try
        {
            var bankOrder = new BankOrder(symbol.Ticker, req.Type, req.Quantity, symbol.LastPrice, paperTrade);
            var result = await adapter.PlaceOrderAsync(bankOrder);

            order.Status = result.Status;
            order.Price = result.FilledPrice;
            order.FilledAt = result.FilledAt;
            await db.SaveChangesAsync();

            if (result.Status == OrderStatus.FILLED)
                await UpsertPositionAsync(userId, symbol.Id, req.Type, req.Quantity, result.FilledPrice);
        }
        catch (Exception ex)
        {
            order.Status = OrderStatus.FAILED;
            await db.SaveChangesAsync();
            throw new AppException($"Order execution failed: {ex.Message}", 500);
        }

        await db.Entry(order).Reference(o => o.Symbol).LoadAsync();
        return ToOrderDto(order);
    }

    public async Task<OrderDto> CancelOrderAsync(Guid userId, Guid orderId)
    {
        var order = await db.Orders.Include(o => o.Symbol).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new AppException("Order not found", 404);

        if (order.UserId != userId) throw new AppException("Not authorized", 403);
        if (order.Status != OrderStatus.PENDING) throw new AppException("Only pending orders can be cancelled");

        order.Status = OrderStatus.CANCELLED;
        await db.SaveChangesAsync();
        return ToOrderDto(order);
    }

    public async Task<PortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var positions = await db.Positions
            .Where(p => p.UserId == userId)
            .Include(p => p.Symbol)
            .ToListAsync();

        var positionDtos = new List<PositionDto>();
        foreach (var pos in positions)
        {
            double currentPrice = pos.Symbol.LastPrice;
            pos.UnrealizedPnl = (currentPrice - pos.AvgPrice) * pos.Quantity;
            pos.UpdatedAt = DateTime.UtcNow;
            positionDtos.Add(ToPositionDto(pos, currentPrice));
        }
        await db.SaveChangesAsync();

        double totalPnl = positionDtos.Sum(p => p.UnrealizedPnl);
        double totalValue = positions.Sum(p => p.Symbol.LastPrice * p.Quantity);
        return new PortfolioDto(positionDtos, totalPnl, totalValue);
    }

    private async Task UpsertPositionAsync(Guid userId, Guid symbolId, OrderType type, double qty, double price)
    {
        var pos = await db.Positions.FirstOrDefaultAsync(p => p.UserId == userId && p.SymbolId == symbolId);
        if (type == OrderType.BUY)
        {
            if (pos != null)
            {
                double total = pos.Quantity + qty;
                pos.AvgPrice = (pos.Quantity * pos.AvgPrice + qty * price) / total;
                pos.Quantity = total;
                pos.UpdatedAt = DateTime.UtcNow;
            }
            else db.Positions.Add(new Position { UserId = userId, SymbolId = symbolId, Quantity = qty, AvgPrice = price });
        }
        else if (pos != null)
        {
            pos.Quantity -= qty;
            pos.UpdatedAt = DateTime.UtcNow;
            if (pos.Quantity <= 0) db.Positions.Remove(pos);
        }
        await db.SaveChangesAsync();
    }

    private static OrderDto ToOrderDto(Order o) => new(
        o.Id, o.UserId, o.SymbolId, o.Type, o.Quantity, o.Price, o.Status,
        o.BankAdapter, o.PaperTrade, o.Notes, o.CreatedAt, o.FilledAt,
        new SymbolDto(o.Symbol.Id, o.Symbol.Ticker, o.Symbol.Name, o.Symbol.Sector, o.Symbol.LastPrice, o.Symbol.UpdatedAt)
    );

    private static PositionDto ToPositionDto(Position p, double currentPrice) => new(
        p.Id, p.UserId, p.SymbolId, p.Quantity, p.AvgPrice, p.UnrealizedPnl, currentPrice,
        p.CreatedAt, p.UpdatedAt,
        new SymbolDto(p.Symbol.Id, p.Symbol.Ticker, p.Symbol.Name, p.Symbol.Sector, p.Symbol.LastPrice, p.Symbol.UpdatedAt)
    );
}
