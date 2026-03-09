namespace TradingBot.DTOs;

public record SymbolDto(Guid Id, string Ticker, string Name, string Sector, double LastPrice, DateTime UpdatedAt);

public record CandleDto(Guid Id, Guid SymbolId, double Open, double High, double Low, double Close, double Volume, DateTime Timestamp);
