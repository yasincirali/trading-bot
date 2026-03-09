namespace TradingBot.Indicators;

public static class EmaIndicator
{
    public static double[] Calculate(double[] prices, int period)
    {
        if (prices.Length == 0) return [];
        double k = 2.0 / (period + 1);
        var ema = new double[prices.Length];
        ema[0] = prices[0];
        for (int i = 1; i < prices.Length; i++)
            ema[i] = prices[i] * k + ema[i - 1] * (1 - k);
        return ema;
    }

    public static IndicatorResult Compute(double[] prices, int shortPeriod = 9, int longPeriod = 21)
    {
        if (prices.Length < longPeriod + 1)
            return new IndicatorResult(SignalType.NEUTRAL, 0, null);

        var shortEma = Calculate(prices, shortPeriod);
        var longEma = Calculate(prices, longPeriod);

        double lastShort = shortEma[^1];
        double lastLong = longEma[^1];
        double diff = (lastShort - lastLong) / lastLong;
        double strength = Math.Min(Math.Abs(diff) * 20, 1.0);

        var value = new { shortEma = lastShort, longEma = lastLong, diff };

        if (lastShort > lastLong) return new IndicatorResult(SignalType.BUY, strength, value);
        if (lastShort < lastLong) return new IndicatorResult(SignalType.SELL, strength, value);
        return new IndicatorResult(SignalType.NEUTRAL, 0, value);
    }
}
