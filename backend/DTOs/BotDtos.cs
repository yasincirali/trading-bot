namespace TradingBot.DTOs;

public record BotConfigDto(
    Guid Id,
    Guid UserId,
    bool Enabled,
    double ConfidenceThreshold,
    double MaxOrderSizeTry,
    double DailyLossLimitTry,
    int TickIntervalMs,
    string[] Watchlist,
    DateTime UpdatedAt
);

public record BotStatusDto(bool Running, BotConfigDto? Config, bool PaperTrading);

public record UpdateBotConfigRequest(
    double? ConfidenceThreshold,
    double? MaxOrderSizeTry,
    double? DailyLossLimitTry,
    int? TickIntervalMs,
    string[]? Watchlist
);
