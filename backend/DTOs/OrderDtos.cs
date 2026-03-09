using System.ComponentModel.DataAnnotations;
using TradingBot.Models;

namespace TradingBot.DTOs;

public record PlaceOrderRequest(
    [Required] string Ticker,
    [Required] OrderType Type,
    [Range(1, double.MaxValue)] double Quantity,
    string? BankAdapter
);

public record OrderDto(
    Guid Id,
    Guid UserId,
    Guid SymbolId,
    OrderType Type,
    double Quantity,
    double Price,
    OrderStatus Status,
    string BankAdapter,
    bool PaperTrade,
    string? Notes,
    DateTime CreatedAt,
    DateTime? FilledAt,
    SymbolDto Symbol
);
