namespace TradingBot.DTOs;

public record PositionDto(
    Guid Id,
    Guid UserId,
    Guid SymbolId,
    double Quantity,
    double AvgPrice,
    double UnrealizedPnl,
    double? CurrentPrice,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SymbolDto Symbol
);

public record PortfolioDto(IEnumerable<PositionDto> Positions, double TotalPnl, double TotalValue);
