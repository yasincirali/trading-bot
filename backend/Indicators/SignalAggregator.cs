namespace TradingBot.Indicators;

public static class SignalAggregator
{
    private const double WeightRsi = 0.30;
    private const double WeightMacd = 0.30;
    private const double WeightBollinger = 0.20;
    private const double WeightEma = 0.20;

    public static AggregatedSignal Aggregate(double[] closePrices)
    {
        var rsi = RsiIndicator.Compute(closePrices);
        var macd = MacdIndicator.Compute(closePrices);
        var bollinger = BollingerBandsIndicator.Compute(closePrices);
        var ema = EmaIndicator.Compute(closePrices);

        double weighted =
            Score(rsi) * WeightRsi +
            Score(macd) * WeightMacd +
            Score(bollinger) * WeightBollinger +
            Score(ema) * WeightEma;

        double confidence = Math.Abs(weighted);
        SignalType signal = weighted > 0.1 ? SignalType.BUY
                         : weighted < -0.1 ? SignalType.SELL
                         : SignalType.NEUTRAL;

        return new AggregatedSignal(signal, confidence, confidence, rsi, macd, bollinger, ema);
    }

    private static double Score(IndicatorResult r) => r.Signal switch
    {
        SignalType.BUY => r.Strength,
        SignalType.SELL => -r.Strength,
        _ => 0
    };
}
