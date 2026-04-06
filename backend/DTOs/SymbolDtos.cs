namespace TradingBot.DTOs;

public record SymbolDto(Guid Id, string Ticker, string Name, string Sector, string Type, double LastPrice, DateTime UpdatedAt);

public record CandleDto(Guid Id, Guid SymbolId, double Open, double High, double Low, double Close, double Volume, DateTime Timestamp);

public record AddSymbolRequest(string Ticker, string? Name, string? Sector, string? Type);
