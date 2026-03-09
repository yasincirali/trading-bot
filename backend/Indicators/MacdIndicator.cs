namespace TradingBot.Indicators;

public static class MacdIndicator
{
    public static IndicatorResult Compute(double[] prices, int fast = 12, int slow = 26, int signal = 9)
    {
        if (prices.Length < slow + signal)
            return new IndicatorResult(SignalType.NEUTRAL, 0, null);

        var fastEma = EmaIndicator.Calculate(prices, fast);
        var slowEma = EmaIndicator.Calculate(prices, slow);

        var macdLine = fastEma.Zip(slowEma, (f, s) => f - s).ToArray();
        var signalLine = EmaIndicator.Calculate(macdLine, signal);

        double lastMacd = macdLine[^1];
        double lastSignal = signalLine[^1];
        double prevMacd = macdLine[^2];
        double prevSignal = signalLine[^2];

        double histogram = lastMacd - lastSignal;
        double prevHistogram = prevMacd - prevSignal;

        double signalRef = Math.Abs(lastSignal) == 0 ? 1 : Math.Abs(lastSignal);
        double strength = Math.Min(Math.Abs(histogram) / signalRef, 1.0);

        var value = new { macd = lastMacd, signal = lastSignal, histogram };

        bool crossedAbove = prevHistogram <= 0 && histogram > 0;
        bool crossedBelow = prevHistogram >= 0 && histogram < 0;

        if (crossedAbove || (histogram > 0 && lastMacd > 0))
            return new IndicatorResult(SignalType.BUY, strength, value);
        if (crossedBelow || (histogram < 0 && lastMacd < 0))
            return new IndicatorResult(SignalType.SELL, strength, value);

        return new IndicatorResult(SignalType.NEUTRAL, strength * 0.5, value);
    }
}
