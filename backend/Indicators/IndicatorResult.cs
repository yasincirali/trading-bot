namespace TradingBot.Indicators;

public enum SignalType { BUY, SELL, NEUTRAL }

public record IndicatorResult(SignalType Signal, double Strength, object? Value);

public record AggregatedSignal(
    SignalType Signal,
    double Strength,
    double Confidence,
    IndicatorResult Rsi,
    IndicatorResult Macd,
    IndicatorResult Bollinger,
    IndicatorResult Ema
);
