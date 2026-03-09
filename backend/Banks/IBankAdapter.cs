using TradingBot.Models;

namespace TradingBot.Banks;

public record BankOrder(string SymbolTicker, OrderType Type, double Quantity, double Price, bool PaperTrade);

public record BankOrderResult(string OrderId, OrderStatus Status, double FilledPrice, DateTime FilledAt, double Commission);

public record BankPosition(string SymbolTicker, double Quantity, double AvgPrice, double CurrentPrice, double UnrealizedPnl);

public record BankBalance(double AvailableTry, double TotalPortfolioValue);

public interface IBankAdapter
{
    string Name { get; }
    Task<BankBalance> GetBalanceAsync();
    Task<BankOrderResult> PlaceOrderAsync(BankOrder order);
    Task<IEnumerable<BankPosition>> GetPositionsAsync();
    Task<bool> CancelOrderAsync(string orderId);
}
