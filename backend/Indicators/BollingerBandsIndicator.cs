namespace TradingBot.Indicators;

public static class BollingerBandsIndicator
{
    public static IndicatorResult Compute(double[] prices, int period = 20, double stdDevMult = 2.0)
    {
        if (prices.Length < period)
            return new IndicatorResult(SignalType.NEUTRAL, 0, null);

        var recent = prices[^period..];
        double sma = recent.Average();
        double variance = recent.Average(p => Math.Pow(p - sma, 2));
        double stdDev = Math.Sqrt(variance);

        double upper = sma + stdDevMult * stdDev;
        double lower = sma - stdDevMult * stdDev;
        double current = prices[^1];
        double bandWidth = upper - lower;

        var value = new { upper, middle = sma, lower, currentPrice = current, bandWidth };

        if (current <= lower)
            return new IndicatorResult(SignalType.BUY, Math.Min((lower - current) / stdDev + 0.1, 1.0), value);

        if (current >= upper)
            return new IndicatorResult(SignalType.SELL, Math.Min((current - upper) / stdDev + 0.1, 1.0), value);

        double position = bandWidth > 0 ? (current - lower) / bandWidth : 0.5;
        if (position < 0.3)
            return new IndicatorResult(SignalType.BUY, (0.3 - position) * 0.5, value);
        if (position > 0.7)
            return new IndicatorResult(SignalType.SELL, (position - 0.7) * 0.5, value);

        return new IndicatorResult(SignalType.NEUTRAL, 0, value);
    }
}
